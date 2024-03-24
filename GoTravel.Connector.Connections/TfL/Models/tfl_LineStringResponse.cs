namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_LineStringResponse
{
    public string lineId { get; set; }
    public string lineName { get; set; }
    public string direction { get; set; }
    public bool isOutboundOnly { get; set; }
    public ICollection<string> lineStrings { get; set; }
    public ICollection<tfl_StopPoint> stations { get; set; } = new List<tfl_StopPoint>();
    public ICollection<tfl_OrderedLineRoute> orderedLineRoutes { get; set; } = new List<tfl_OrderedLineRoute>();
}

public class tfl_OrderedLineRoute
{
    public string name { get; set; }
    public string serviceType { get; set; }
    public ICollection<string> naptanIds { get; set; }
}