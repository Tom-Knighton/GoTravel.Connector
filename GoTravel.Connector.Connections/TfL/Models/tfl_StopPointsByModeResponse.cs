namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_StopPointsByModeResponse
{
    public ICollection<tfl_StopPoint> stopPoints { get; set; }
    public int total { get; set; }
}