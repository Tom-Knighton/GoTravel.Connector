namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_LineMode
{
    public string modeName { get; set; }
    public bool isTflService { get; set; }
    public bool isFarePaying { get; set; }
    public bool isScheduledService { get; set; }
}