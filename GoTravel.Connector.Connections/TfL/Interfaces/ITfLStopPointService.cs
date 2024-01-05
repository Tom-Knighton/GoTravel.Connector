using System.Collections;
using GoTravel.Connector.Connections.TfL.Models;

namespace GoTravel.Connector.Connections.TfL.Interfaces;

public interface ITfLStopPointService
{
    Task<ICollection<tfl_StopPoint>> RetrieveNonBusStopPoints();
    Task<ICollection<tfl_StopPoint>> RetrieveBusStopPoints();
}