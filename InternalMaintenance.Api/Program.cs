using DotNetEnv;
using InternalMaintenance.Api.Extensions;

try
{
    // Load local environment values before configuration is built so VS / dotnet run can see them.
    Env.Load(".env", Env.TraversePath());
}
catch (FileNotFoundException)
{
    // Safe to continue when .env is absent.
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await app.UseApplicationPipelineAsync();

app.Run();
