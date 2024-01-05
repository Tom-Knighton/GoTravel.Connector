using System.Text.Json;
using GoTravel.Connector.Connections.Exceptions;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Standard.Models.MessageModels;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TfLStopPointService(ITfLLineService _lineService, IHttpClientFactory api) : IGenericStopPointService, ITfLStopPointService
{
    private const string OperatorPrefix = "tfl-";
    private readonly HttpClient _api = api.CreateClient("TfLAPI");

    private readonly HashSet<string> StopTypes = new(StringComparer.InvariantCultureIgnoreCase) { "NaptanPublicBusCoachTram", "NaptanRailStation", "NaptanMetroStation", "TransportInterchange" };
    private readonly HashSet<string> BusStopTypes = new(StringComparer.InvariantCultureIgnoreCase) { "NaptanPublicBusCoachTram" };
    private readonly HashSet<string> BusStopModes = new(StringComparer.InvariantCultureIgnoreCase) { "bus" };

    public async Task<ICollection<StopPointUpdateDto>> GetStopPointUpdateDtos(GetStopPointDtoType type = GetStopPointDtoType.All, CancellationToken ct = default)
    {
        var stopPointTasks = new List<Task<ICollection<tfl_StopPoint>>>();
        
        if (type is GetStopPointDtoType.All or GetStopPointDtoType.BusOnly)
        {
            stopPointTasks.Add(RetrieveBusStopPoints());
        }

        if (type is GetStopPointDtoType.All or GetStopPointDtoType.NonBusOnly)
        {
            stopPointTasks.Add(RetrieveNonBusStopPoints());
        }

        var stopPoints = (await Task.WhenAll(stopPointTasks)).SelectMany(r => r);

        var dtos = stopPoints.Select(stop => new StopPointUpdateDto
        {
            Id = stop.id,
            Name = stop.commonName,
            ParentId = stop.parentId,
            HubId = stop.hubNaptanCode,
            Indicator = stop.indicator,
            Letter = stop.stopLetter,
            SMS = stop.smsCode,
            Latitude = stop.lat,
            Longitude = stop.lon,
            Lines = stop.lineModeGroups.SelectMany(g => g.lineIdentifier.Select(l => OperatorPrefix + l)).ToList()
        });

        return dtos.ToList();
    }

    public async Task<ICollection<tfl_StopPoint>> RetrieveNonBusStopPoints()
    {
        var stopPoints = new List<tfl_StopPoint>();
        var modes = await _lineService.RetrieveAllLineModes();

        var nonBusTypes = StopTypes.Where(t => BusStopTypes.Contains(t) == false).ToHashSet();
        var nonBusModes = modes.Where(m => m.isTflService && BusStopModes.Contains(m.modeName) == false).Select(m => m.modeName).ToHashSet();
        
        stopPoints.AddRange(await StopPointsForTypes(nonBusTypes, nonBusModes));
        
        return stopPoints;
    }

    public async Task<ICollection<tfl_StopPoint>> RetrieveBusStopPoints()
    {
        var stopPoints = new List<tfl_StopPoint>();

        var searchResults = await StopPointsForTypes(BusStopTypes, BusStopModes);
        
        stopPoints.AddRange(searchResults);

        return stopPoints;
    }

    private async Task<ICollection<tfl_StopPoint>> StopPointsForTypes(IEnumerable<string> types, ICollection<string> allowedModes)
    {
        var stopPoints = new List<tfl_StopPoint>();
        try
        {
            var page = 1;
            var typeString = string.Join(',', types);
            var reachedEnd = false;

            while (!reachedEnd)
            {
                using var response = await _api.GetAsync($"StopPoint/Type/{typeString}/page/{page}", HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }
                
                await using var stream = await response.Content.ReadAsStreamAsync();
                var results = await JsonSerializer.DeserializeAsync<ICollection<tfl_StopPoint>>(stream);
                if (results is null || results.Count == 0)
                {
                    reachedEnd = true;
                    continue;
                }

                foreach (var result in results)
                {
                    result.lineModeGroups = result.lineModeGroups.Where(lmg => allowedModes.Contains(lmg.modeName));
                    result.children = result.children.Where(c => c.lineModeGroups.Any() && string.IsNullOrWhiteSpace(c.stopType) || types.Contains(c.stopType));
                }
                stopPoints.AddRange(results.Where(s => s.lineModeGroups.Any()));

                page++;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR" + ex.Message); //TODO: LOG
        }
        
        return stopPoints;
    }
}