using VietnamDiscovery.Api.Services.Interfaces;

namespace VietnamDiscovery.Api.Services.Implementations;

public class HealthService : IHealthService
{
    public object GetStatus()
    {
        return new
        {
            Status = "Healthy",
            Application = "Vietnam Discovery API",
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}