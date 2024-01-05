using GoTravel.Standard.Models.MessageModels;

namespace GoTravel.Connector.Domain.Interfaces;

public enum GetStopPointDtoType
{
    BusOnly,
    NonBusOnly,
    All
}

public interface IGenericStopPointService
{
    Task<ICollection<StopPointUpdateDto>> GetStopPointUpdateDtos(GetStopPointDtoType type = GetStopPointDtoType.All, CancellationToken ct = default);
}