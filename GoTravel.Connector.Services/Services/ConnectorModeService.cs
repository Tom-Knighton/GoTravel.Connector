using GoTravel.Connector.Domain.Enums;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Messages;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.Services;

public class ConnectorModeService: IConnectorModeService
{
    private readonly IPublishEndpoint _publisher;
    private readonly HashSet<IGenericLinesService> _connections;
    
    public ConnectorModeService(IPublishEndpoint publisher, IServiceProvider provider)
    {
        _publisher = publisher;
        _connections = new HashSet<IGenericLinesService>();
        
        var operators = Enum.GetValues<ConnectionOperator>();
        foreach (var conn in operators)
        {
            try
            {
                _connections.Add(provider.GetRequiredKeyedService<IGenericLinesService>(conn));
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
    
    public async Task FetchAndSendAllModesAndLines(CancellationToken ct)
    {
        foreach (var conn in _connections)
        {
            var dtos = await conn.GetLineModeDtos(ct);
            var publishTasks = dtos.Select(x => _publisher.Publish<ILineModeUpdated>(new ILineModeUpdated(x), ct)).ToList();
            await Task.WhenAll(publishTasks);
        }
    }

    public async Task FetchAndSendAllRouteStrings(CancellationToken ct)
    {
        foreach (var conn in _connections)
        {
            var dtos = await conn.GetLineRouteDtos(ct);
            var publishTasks = dtos.Select(x => _publisher.Publish(new ILineStringUpdated(x), ct)).ToList();
            await Task.WhenAll(publishTasks);
        }
    }
}