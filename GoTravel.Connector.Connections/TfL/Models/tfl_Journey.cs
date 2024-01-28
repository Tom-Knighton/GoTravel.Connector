namespace GoTravel.Connector.Connections.TfL.Models;

public class tfl_JourneyResult
{
    public ICollection<tfl_Journey> journeys { get; set; }
}

public class tfl_Journey
{
    public DateTime startDateTime { get; set; }
    public DateTime arrivalDateTime { get; set; }
    public double duration { get; set; }
    public bool alternativeRoute { get; set; }

    public ICollection<tfl_JourneyLeg> legs { get; set; } = new List<tfl_JourneyLeg>();
}

public class tfl_JourneyLeg
{
    public double duration { get; set; }
    public DateTime? departureTime { get; set; }
    public DateTime? arrivalTime { get; set; }
    public DateTime scheduledDepartureTime { get; set; }
    public DateTime scheduledArrivalTime { get; set; }
    public bool isDisrupted { get; set; }
    public bool hasFixedLocations { get; set; }
    
    public tfl_JourneyLegInstruction instruction { get; set; }
    public tfl_StopPoint? arrivalPoint { get; set; }
    public tfl_StopPoint? departurePoint { get; set; }
    public tfl_JourneyLegPath path { get; set; }
    public tfl_JourneyLegMode mode { get; set; }
    public ICollection<tfl_JourneyLegRouteOption> routeOptions { get; set; }
}

public class tfl_JourneyLegInstruction
{
    public string summary { get; set; }
    public string detailed { get; set; }

    public ICollection<tfl_JourneyLegInstructionStep> steps { get; set; } = new List<tfl_JourneyLegInstructionStep>();
}

public class tfl_JourneyLegInstructionStep
{
    public string description { get; set; }
    public string turnDirection { get; set; }
    public double distance { get; set; }
    public double cumulativeDistance { get; set; }
    public string skyDirectionDescription { get; set; }
    public double latitude { get; set; }
    public double longitude { get; set; }
    public string descriptionHeading { get; set; }
}

public class tfl_JourneyLegPath {
    
    public string lineString { get; set; }
    public ICollection<tfl_JourneyLegStop> stopPoints { get; set; }
}

public class tfl_JourneyLegStop
{
    public string id { get; set; }
    public string name { get; set; }
}

public class tfl_JourneyLegRouteOption
{
    public string name { get; set; }
    public ICollection<string> directions { get; set; }
    public string direction { get; set; }
    public tfl_JourneyLegRouteOptionLine? lineIdentifier { get; set; }
}

public class tfl_JourneyLegRouteOptionLine
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}

public class tfl_JourneyLegMode
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string routeType { get; set; }
    public string status { get; set; }
    public string motType { get; set; }
    public string network { get; set; }
}