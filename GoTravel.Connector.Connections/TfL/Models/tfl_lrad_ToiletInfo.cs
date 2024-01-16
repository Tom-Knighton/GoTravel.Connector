namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_lrad_ToiletInfo
{
    public string StationUniqueId { get; set; }
    public string IsAccessible { get; set; }
    public string HasBabyChanging { get; set; }
    public string IsInsideGateLine { get; set; }
    public string Location { get; set; }
    public string IsFeeCharged { get; set; }
    public string Type { get; set; }
}