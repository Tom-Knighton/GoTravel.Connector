using GoTravel.Connector.Connections.TfL.Models;

namespace GoTravel.Connector.Connections.TfL.Interfaces;

public interface ITfLLineService
{
    Task<ICollection<tfl_LineMode>> RetrieveAllLineModes();
    Task<ICollection<tfl_Line>> RetrieveAllLinesForMode(string lineMode);
}