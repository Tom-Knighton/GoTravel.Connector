namespace GoTravel.Connector.Services.Interfaces;

public interface IConnectorStopService
{
    Task FetchAndSendNonBusStopPointUpdates(CancellationToken ct);
    Task FetchAndSendBusStopPointUpdates(CancellationToken ct);
    Task FetchAndSendStopPointInfo(CancellationToken ct);
}