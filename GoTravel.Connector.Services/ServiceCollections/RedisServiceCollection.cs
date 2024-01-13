using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace GoTravel.Connector.Services.ServiceCollections;

public static class RedisServiceCollection
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["Host"];
        var port = int.Parse(configuration["Port"] ?? "6379");
        var db = configuration["Database"];
        var pass = configuration["Password"];
        
        var options = new ConfigurationOptions
        {
            EndPoints = { { host, port } },
            Password = pass,
            DefaultDatabase = int.Parse(db),
            ClientName = "GTCON",
        };

        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddScoped<IDatabase>(_ => multiplexer.GetDatabase());

        var arrivalSchema = new Schema()
            .AddTagField(new FieldName("$.StopPointId", "stopPointId"))
            .AddTagField(new FieldName("$.Operator", "operator"));

        var ft = multiplexer.GetDatabase().FT();

        try
        {
            ft.DropIndex("idx:stopOperatorArrivals");
        }
        catch (Exception ex)
        {
            var x = 1;
        }
        
        multiplexer
            .GetDatabase()
            .FT()
            .Create("idx:stopOperatorArrivals", new FTCreateParams().On(IndexDataType.JSON).Prefix("stopOperatorArrivals"), arrivalSchema);
        
        return services;
    }
}