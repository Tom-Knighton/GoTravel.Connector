using GoTravel.Connector.Domain.Enums;
using GoTravel.Connector.Domain.Exceptions;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Models.Journeys;
using Microsoft.Extensions.DependencyInjection;

namespace GoTravel.Connector.Services.Services;

public class ConnectorJourneyService: IConnectorJourneyService
{
    private readonly Dictionary<ConnectionOperator, IGenericJourneyService> _connections;
    
    public ConnectorJourneyService(IServiceProvider provider)
    {
        _connections = new Dictionary<ConnectionOperator, IGenericJourneyService>();

        var operators = Enum.GetValues<ConnectionOperator>();
        foreach (var conn in operators)
        {
            try
            {
                _connections.Add(conn, provider.GetRequiredKeyedService<IGenericJourneyService>(conn));
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
    
    public async Task<ICollection<Journey>> GetPossibleJourneys(JourneyRequest request, HashSet<string>? excludedConnections = null, CancellationToken ct = default)
    {
        var timeout = TimeSpan.FromSeconds(30);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(timeout);
        var tasks = _connections
            .Where(c => excludedConnections is null || !excludedConnections.Contains(c.Key.ToName()))
            .Select(c => c.Value.GetJourneyOptionDtos(request, linkedCts.Token))
            .ToList();
        var journeyTasks = Task.WhenAll(tasks);

        await Task.WhenAny(journeyTasks, Task.Delay(Timeout.Infinite, linkedCts.Token));
        
        var completedResults = tasks
            .Where(t => t.IsCompletedSuccessfully)
            .SelectMany(t => t.Result)
            .ToList();

        return completedResults;
    }

    public async Task<ICollection<Journey>> GetPossibleJourneys(string connection, JourneyRequest request, CancellationToken ct = default)
    {
        var service = _connections
            .FirstOrDefault(k => k.Key.ToName().Equals(connection, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (service is null)
        {
            throw new ConnectionDoesntExistException(connection);
        }

        var journeys = await service.GetJourneyOptionDtos(request, ct);

        return journeys;
    }
}