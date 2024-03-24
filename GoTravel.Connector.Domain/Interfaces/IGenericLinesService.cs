using GoTravel.Standard.Models.MessageModels;

namespace GoTravel.Connector.Domain.Interfaces;

public interface IGenericLinesService
{
    Task<ICollection<LineModeUpdateDto>> GetLineModeDtos(CancellationToken ct);
    Task<ICollection<LineStringUpdateDto>> GetLineRouteDtos(CancellationToken ct);
}