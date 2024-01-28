using GoTravel.Standard.Models.Journeys;

namespace GoTravel.Connector.Services.Interfaces;

public interface IConnectorJourneyService
{
    /// <summary>
    /// Returns journey options for all connections except those excluded
    /// </summary>
    public Task<ICollection<Journey>> GetPossibleJourneys(JourneyRequest request, HashSet<string>? excludedConnections = null, CancellationToken ct = default);

    /// <summary>
    /// Returns journeys for a specified connection only
    /// </summary>
    public Task<ICollection<Journey>> GetPossibleJourneys(string connection, JourneyRequest request, CancellationToken ct = default);
}