using GoTravel.Connector.Domain.Enums;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Messages;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.Services;

public class ConnectorStopService: IConnectorStopService
{
    private readonly IPublishEndpoint _publisher;
    private readonly HashSet<IGenericStopPointService> _connections;
    
    public ConnectorStopService(IPublishEndpoint publisher, IServiceProvider provider)
    {
        _publisher = publisher;
        _connections = new HashSet<IGenericStopPointService>();
        
        var operators = Enum.GetValues<ConnectionOperator>();
        foreach (var conn in operators)
        {
            try
            {
                _connections.Add(provider.GetRequiredKeyedService<IGenericStopPointService>(conn));
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }

    public async Task FetchAndSendNonBusStopPointUpdates(CancellationToken ct)
    {
        foreach (var conn in _connections)
        {
            var updates = await conn.GetStopPointUpdateDtos(GetStopPointDtoType.NonBusOnly, ct);
            var publishTasks = updates.Select(x => _publisher.Publish(new IStopPointUpdated(x), ct));
            await Task.WhenAll(publishTasks);
        }
    }

    public async Task FetchAndSendBusStopPointUpdates(CancellationToken ct)
    {
        foreach (var conn in _connections)
        {
            var updates = await conn.GetStopPointUpdateDtos(GetStopPointDtoType.BusOnly, ct);
            var publishTasks = updates.Select(x => _publisher.Publish(new IStopPointUpdated(x), ct));
            await Task.WhenAll(publishTasks);
        }
    }
}