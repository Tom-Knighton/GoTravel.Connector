using System.Text.Json;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Standard.Models.Arrivals;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TfLArrivalService(ITfLLineService lineService, IHttpClientFactory api): ITfLArrivalService, IGenericGeneralArrivalService
{
    private const string OperatorPrefix = "tfl-";
    private const int ArrivalForStopCount = 50;
    private const int ArrivalForBusStopCount = 10;
    private readonly HttpClient _api = api.CreateClient("TfLAPI");
    private readonly HashSet<string> ExcludedLineModes = new(StringComparer.InvariantCultureIgnoreCase) { "elizabeth-line", "overground" };
    private readonly HashSet<string> BusLineModes = new(StringComparer.InvariantCultureIgnoreCase) { "bus" };
    
    public async Task<ICollection<tfl_Arrival>> GetAllModeArrivals(CancellationToken ct = default)
    {
        var modes = await lineService.RetrieveAllLineModes();
        var filteredModes = modes
            .Where(m => m.isTflService && ExcludedLineModes.Contains(m.modeName) == false)
            .Select(m => m.modeName)
            .ToList();

        var arrivals = new List<tfl_Arrival>();
        var tasks = filteredModes.Select(async mode =>
        {
            var count = BusLineModes.Contains(mode) ? ArrivalForBusStopCount : ArrivalForStopCount;
            await using var response = await _api.GetStreamAsync($"Mode/{mode}/Arrivals?count={count}", ct);
            var result = await JsonSerializer.DeserializeAsync<ICollection<tfl_Arrival>>(response, cancellationToken: ct);
            arrivals.AddRange(result ?? new List<tfl_Arrival>());
        });

        await Task.WhenAll(tasks);

        return arrivals;
    }

    public async Task<ICollection<ArrivalDeparture>> GetArrivalDepartureDtos(CancellationToken ct)
    {
        var arrivals = await GetAllModeArrivals(ct);

        var dtos = arrivals.Select(x =>
        {
            var id = $"tfl_{x.id}_{x.platformName}_{x.expectedArrival:hh:mm:ss}";
            
            return new ArrivalDeparture
            {
                OperatorsArrivalId = id,
                Operator = "tfl",
                VehicleId = x.vehicleId,
                IsCancelled = false,
                IsDelayed = false,
                StopId = x.naptanId,
                DestinationStopId = x.destinatioNaptanId ?? "",
                DestinationName = x.destinationName,
                PredictedArrival = x.expectedArrival,
                Line = !string.IsNullOrWhiteSpace(x.lineId) ? OperatorPrefix + x.lineId : string.Empty,
                LineMode = !string.IsNullOrWhiteSpace(x.modeName)? OperatorPrefix + x.modeName : string.Empty,
                Direction = x.direction,
                Platform = x.platformName
            };
        });

        return dtos.ToList();
    }
}