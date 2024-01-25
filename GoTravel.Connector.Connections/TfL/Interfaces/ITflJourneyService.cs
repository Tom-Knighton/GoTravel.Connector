using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Standard.Models.Journeys;

namespace GoTravel.Connector.Connections.TfL.Interfaces;

public interface ITflJourneyService
{
    public Task<ICollection<tfl_Journey>> GetTflJourneys(JourneyRequest request, CancellationToken ct = default);
}