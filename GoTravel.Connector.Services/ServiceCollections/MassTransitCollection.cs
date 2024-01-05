using System.Text.Json.Serialization;
using MassTransit;
using MassTransit.Serialization.JsonConverters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.ServiceCollections;

public static class MassTransitCollection
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(mt =>
        {
            mt.UsingRabbitMq((ctx, cfg) =>
            {
                var host = configuration["Host"];
                var vh = configuration["VirtualHost"];
                var port = ushort.Parse(configuration["Port"]);
                cfg.Host(host, port, vh, c =>
                {
                    var username = configuration["User"];
                    var password = configuration["Password"];
                    c.Username(username);
                    c.Password(password);
                });
                
                cfg.ConfigureJsonSerializerOptions(opt =>
                {
                    var converters = opt.Converters.Where(x => x is not StringDecimalJsonConverter).ToList();
                    opt.Converters.Clear();
                    foreach (var converter in converters)
                    {
                        opt.Converters.Add(converter);
                    }
                    return opt;
                });
            });
        });
        
        return services;
    }
}