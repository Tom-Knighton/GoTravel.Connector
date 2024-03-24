using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GoTravel.Connector.Services.ServiceCollections;

public static class SerilogCollection
{
    public static IServiceCollection AddLogs(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        return services;
    }
}