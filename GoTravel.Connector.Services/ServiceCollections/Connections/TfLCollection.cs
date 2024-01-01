using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.ServiceCollections.Connections;

public static class TfLCollection
{
    public static IServiceCollection AddTfLServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ITfLLineService, TfLLineService>();

        services.AddHttpClient("TfLAPI", c =>
        {
            c.BaseAddress = new Uri(configuration["BaseUrl"]);

            c.DefaultRequestHeaders.Add("User-Agent", "GoTravel.Connector");
            c.DefaultRequestHeaders.Add("app_key", configuration["Key"]);
        });
        
        return services;
    }
}