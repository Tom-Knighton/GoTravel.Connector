using GoTravel.Standard.Models.Arrivals;

namespace GoTravel.Connector.Domain.Interfaces;

public interface IGenericGeneralArrivalService
{
    Task<ICollection<ArrivalDeparture>> GetArrivalDepartureDtos(CancellationToken ct);
}