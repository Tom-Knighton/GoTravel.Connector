using GoTravel.Connector.Connections.TfL.Models;

namespace GoTravel.Connector.Connections.TfL.Interfaces;

public interface ITfLArrivalService
{
    Task<ICollection<tfl_Arrival>> GetAllModeArrivals(CancellationToken ct = default);
}