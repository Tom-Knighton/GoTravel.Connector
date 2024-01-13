namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_StopPoint
{
    public string naptanId { get; set; }
    public string? indicator { get; set; }
    public string? stopLetter { get; set; }
    public string? smsCode { get; set; }
    public string? stopType { get; set; }
    public string? hubNaptanCode { get; set; }
    public string id { get; set; }
    public string commonName { get; set; }
    public string? parentId { get; set; }

    public double lat { get; set; }
    public double lon { get; set; }
    public IEnumerable<tfl_LineModeGroup> lineModeGroups { get; set; } = new List<tfl_LineModeGroup>();

    public IEnumerable<tfl_StopPoint> children { get; set; } = new List<tfl_StopPoint>();
}