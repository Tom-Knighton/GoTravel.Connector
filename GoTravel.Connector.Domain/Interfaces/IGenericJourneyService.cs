using GoTravel.Standard.Models.Journeys;

namespace GoTravel.Connector.Domain.Interfaces;

public interface IGenericJourneyService
{
    Task<ICollection<Journey>> GetJourneyOptionDtos(JourneyRequest request, CancellationToken ct = default);
}