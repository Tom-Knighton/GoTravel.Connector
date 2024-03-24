namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_LineModeRoutes
{
    public string id { get; set; }
    public string name { get; set; }
    public string modeName { get; set; }
    public DateTime created { get; set; }
    public DateTime? updated { get; set; }
    public ICollection<tfl_LineModeRouteSection> routeSections { get; set; } = new List<tfl_LineModeRouteSection>();
}

public class tfl_LineModeRouteSection
{
    public string name { get; set; }
    public string direction { get; set; }
    public string originationName { get; set; }
    public string destinatioName { get; set; }
    public string originator { get; set; }
    public string destination { get; set; }
    public DateTime validTo { get; set; }
    public DateTime validFrom { get; set; }
}