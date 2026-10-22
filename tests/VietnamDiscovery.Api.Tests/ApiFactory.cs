using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using VietnamDiscovery.Api.Services.Interfaces;

namespace VietnamDiscovery.Api.Tests;

public sealed class ApiFactory(string environment = "Development", bool throwOnHealth = false,
    bool enableTestEndpoints = false)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Diagnostics:EnableTestEndpoints"] = enableTestEndpoints.ToString()
            }));
        builder.ConfigureServices(services =>
        {
            // The validation endpoint exists only in the test host, never in the API project.
            services.AddControllers().AddApplicationPart(typeof(ValidationProbeController).Assembly);

            if (throwOnHealth)
            {
                services.RemoveAll<IHealthService>();
                services.AddScoped<IHealthService, FailingHealthService>();
            }
        });
    }

    private sealed class FailingHealthService : IHealthService
    {
        public object GetStatus() => throw new InvalidOperationException("SECRET_TEST_EXCEPTION");
    }
}
