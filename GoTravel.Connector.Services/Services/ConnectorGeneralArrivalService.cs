using System.Text.Json;
using GoTravel.Connector.Domain.Enums;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Models.Arrivals;
using Microsoft.Extensions.DependencyInjection;
using NRedisStack;
using NRedisStack.Json.DataTypes;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using StackExchange.Redis;

namespace GoTravel.Connector.Services.Services;

public class ConnectorGeneralArrivalService: IConnectorGeneralArrivalService
{
    private readonly HashSet<IGenericGeneralArrivalService> _connections;
    private readonly IDatabase _cache;
    
    public ConnectorGeneralArrivalService(IDatabase cache, IServiceProvider provider)
    {
        _cache = cache;
        _connections = new HashSet<IGenericGeneralArrivalService>();

        var operators = Enum.GetValues<ConnectionOperator>();
        foreach (var conn in operators)
        {
            try
            {
                _connections.Add(provider.GetRequiredKeyedService<IGenericGeneralArrivalService>(conn));
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }

    
    public async Task FetchAllGeneralArrivals(CancellationToken ct)
    {
        var arrivalTasks = _connections.Select(x => x.GetArrivalDepartureDtos(ct));
        var operatorArrivals = (await Task.WhenAll(arrivalTasks)).ToList();


        var toAdd = new List<CacheStopOperatorArrivals>();
        foreach (var stopPoints in operatorArrivals.Select(operatorGroup => operatorGroup.GroupBy(x => x.StopId)))
        {
            toAdd.AddRange(stopPoints.Select(x => new CacheStopOperatorArrivals
            {
                Operator = x.FirstOrDefault()?.Operator ?? "UNK",
                StopPointId = x.Key,
                ArrivalDepartures = x.ToList()
            }));
        }

        var batches = toAdd.Chunk(3000);
        foreach (var batch in batches)
        {
            var pipeline = new Pipeline(_cache);

            var kpvs = batch.Select(a => new KeyPathValue($"stopOperatorArrivals:{a.StopPointId}_{a.Operator}", "$", a));
            pipeline.Json.MSetAsync(kpvs.ToArray());
            foreach (var task in kpvs)
            {
                pipeline.Db.KeyExpireAsync(task.Key, new TimeSpan(0, 0, 1, 0));
            }
            pipeline.Execute();
        }
    }

    public async Task<ICollection<ArrivalDeparture>> GetArrivalsForStopAsync(string stopId, CancellationToken ct = default)
    {
        var ft = _cache.FT();

        var results = await ft.SearchAsync("idx:stopOperatorArrivals", new Query($"@stopPointId:{{{stopId}}}"));
        var arrivals = results
            .ToJson()
            .Select(x => JsonSerializer.Deserialize<CacheStopOperatorArrivals>(x))
            .Where(a => a is not null)
            .SelectMany(a => a?.ArrivalDepartures ?? new List<ArrivalDeparture>())
            .ToList();

        return arrivals;
    }
}