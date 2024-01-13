namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_Arrival
{
    public string id { get; set; }
    public string? vehicleId { get; set; }
    public string naptanId { get; set; }
    public string? stationName { get; set; }
    public string? lineId { get; set; }
    public string? lineName { get; set; }
    public string? modeName { get; set; }
    public string? destinatioNaptanId { get; set; }
    public string direction { get; set; }
    public string? destinationName { get; set; }
    public DateTime expectedArrival { get; set; }
    public string? towards { get; set; }
    public string? platformName { get; set; }

}