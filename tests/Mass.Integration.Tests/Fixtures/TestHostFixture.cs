using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mass.Core.Extensions;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Telemetry;

namespace Mass.Integration.Tests.Fixtures;

public class TestHostFixture : IDisposable
{
    public IHost Host { get; }
    public IServiceProvider Services => Host.Services;
    
    public TestHostFixture()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Add Mass.Core services
                services.AddMassCoreServices();
                
                // Add test logging
                services.AddSingleton<ILogService, TestLogService>();
                
                // Add test telemetry
                services.AddSingleton<ITelemetryService, TestTelemetryService>();
            })
            .Build();
    }
    
    public void Dispose()
    {
        Host?.Dispose();
    }
}
