using GoTravel.Standard.Models;

namespace GoTravel.Connector.Domain.Models;

public class StopInfo
{
    public string StopId { get; set; }
    public List<KeyValuePair<StopPointInfoKey, string>> Infos { get; set; }
}