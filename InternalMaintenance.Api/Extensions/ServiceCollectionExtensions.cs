using System.Text;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Auth;
using InternalMaintenance.Api.Modules.TicketAttachments;
using InternalMaintenance.Api.Modules.TicketAttachments.Storage;
using InternalMaintenance.Api.Services;
using InternalMaintenance.Api.Services.Implementation;
using InternalMaintenance.Api.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InternalMaintenance.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicyName = "FrontendCors";

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing connection string: ConnectionStrings:DefaultConnection (or environment variable ConnectionStrings__DefaultConnection)");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Health Checks: Giám sát trạng thái API và kết nối SQL Server
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            allowedOrigins = ["http://localhost:5173", "https://office-maintenance.vercel.app"];
        }

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var jwtKey = GetRequiredConfigurationValue(configuration, "Jwt:Key");
        var jwtIssuer = GetRequiredConfigurationValue(configuration, "Jwt:Issuer");
        var jwtAudience = GetRequiredConfigurationValue(configuration, "Jwt:Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new InternalMaintenance.Api.Common.UtcDateTimeJsonConverter());
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddHttpClient();
        services.AddHttpClient(ResendEmailService.HttpClientName);
        services.AddHttpContextAccessor();

        services.Configure<R2AttachmentStorageOptions>(configuration.GetSection("R2"));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketAttachmentsService, TicketAttachmentsService>();
        services.AddScoped<IAttachmentStorageService, R2AttachmentStorageService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ITicketCodeGenerator, TicketCodeGenerator>();
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddScoped<InternalMaintenance.Api.Modules.Reports.Services.ReportExportService>();
        services.AddHostedService<SlaMonitorWorker>();

        return services;
    }

    private static string GetRequiredConfigurationValue(
        IConfiguration configuration,
        string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is missing from configuration/environment variables");
        }

        return value;
    }
}
