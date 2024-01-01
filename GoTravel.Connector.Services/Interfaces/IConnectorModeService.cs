namespace GoTravel.Connector.Services.Interfaces;

public interface IConnectorModeService
{
    Task FetchAndSendAllModesAndLines(CancellationToken ct);
}