using System.Collections;
using GoTravel.Connector.Connections.TfL.Models;

namespace GoTravel.Connector.Connections.TfL.Interfaces;

public interface ITfLLineService
{
    Task<ICollection<tfl_LineMode>> RetrieveAllLineModes(CancellationToken ct = default);
    Task<ICollection<tfl_Line>> RetrieveAllLinesForMode(string lineMode, CancellationToken ct = default);
    Task<ICollection<tfl_LineStringResponse?>> RetrieveLineStrings(CancellationToken ct = default);
}