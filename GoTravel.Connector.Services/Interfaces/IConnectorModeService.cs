using GoTravel.Standard.Models.MessageModels;

namespace GoTravel.Connector.Services.Interfaces;

public interface IConnectorModeService
{
    Task FetchAndSendAllModesAndLines(CancellationToken ct);
    Task FetchAndSendAllRouteStrings(CancellationToken ct);
}