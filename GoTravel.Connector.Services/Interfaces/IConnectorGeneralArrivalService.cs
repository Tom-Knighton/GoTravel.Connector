using GoTravel.Standard.Models.Arrivals;

namespace GoTravel.Connector.Services.Interfaces;

public interface IConnectorGeneralArrivalService
{
    Task FetchAllGeneralArrivals(CancellationToken ct);
    Task<ICollection<ArrivalDeparture>> GetArrivalsForStopAsync(string stopId, CancellationToken ct = default);
}