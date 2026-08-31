using InternalMaintenance.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string FrontendCorsPolicyName = "FrontendCors";

    public static async Task<WebApplication> UseApplicationPipelineAsync(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            // Chi seed khi bang tuong ung chua co du lieu (tranh ghi de du lieu cu)
            await SeedData.InitializeAsync(dbContext);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex,
                "Database migration/seed failed during startup. The API will keep running, but DB-backed endpoints may fail.");
        }

        app.UseCors(FrontendCorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Health Check Endpoint (Giám sát trạng thái API và kết nối Database)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                    components = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsJsonAsync(payload);
            }
        });

        app.MapGet("/", () => Results.Ok(new
        {
            service = "Internal Maintenance API",
            status = "running",
            environment = app.Environment.EnvironmentName
        }));

        return app;
    }
}
