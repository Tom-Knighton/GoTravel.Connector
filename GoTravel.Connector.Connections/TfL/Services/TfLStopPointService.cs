using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using CsvHelper;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Connector.Domain.Models;
using GoTravel.Standard.Models;
using GoTravel.Standard.Models.MessageModels;
using NUnit.Framework;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TfLStopPointService(ITfLLineService _lineService, IHttpClientFactory api)
    : IGenericStopPointService, ITfLStopPointService
{
    private const string OperatorPrefix = "tfl-";
    private readonly HttpClient _api = api.CreateClient("TfLAPI");

    private readonly HashSet<string> StopTypes = new(StringComparer.InvariantCultureIgnoreCase)
        { "NaptanPublicBusCoachTram", "NaptanRailStation", "NaptanMetroStation", "TransportInterchange" };

    private readonly HashSet<string> BusStopTypes = new(StringComparer.InvariantCultureIgnoreCase)
        { "NaptanPublicBusCoachTram" };

    private readonly HashSet<string> BusStopModes = new(StringComparer.InvariantCultureIgnoreCase) { "bus" };


    public async Task<ICollection<StopPointUpdateDto>> GetStopPointUpdateDtos(
        GetStopPointDtoType type = GetStopPointDtoType.All, CancellationToken ct = default)
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
        var nonBusModes = modes.Where(m => m.isTflService && BusStopModes.Contains(m.modeName) == false)
            .Select(m => m.modeName).ToHashSet();

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


    public async Task<ICollection<StopInfo>> GetStopPointInfoKvps(CancellationToken ct = default)
    {
        var stations = new List<tfl_lrad_StationInfo>();
        var toilets = new List<tfl_lrad_ToiletInfo>();
        var lifts = new List<tfl_lrad_Lift>();
        var results = new List<StopInfo>();

        try
        {
            var zipStream = await _api.GetStreamAsync("stationdata/tfl-stationdata-detailed.zip", ct);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            foreach (var file in archive.Entries)
            {
                switch (file.Name)
                {
                    case "Stations.csv":
                        stations = await DecodeCsv<tfl_lrad_StationInfo>(file);
                        break;
                    case "Toilets.csv":
                        toilets = await DecodeCsv<tfl_lrad_ToiletInfo>(file);
                        break;
                    case "Lifts.csv":
                        lifts = await DecodeCsv<tfl_lrad_Lift>(file);
                        break;
                    default:
                        break;
                }
            }

            results = stations
                .Select(s =>
                {
                    var info = new StopInfo
                    {
                        StopId = s.UniqueId,
                        Infos = new List<KeyValuePair<StopPointInfoKey, string>>()
                    };

                    info.Infos.AddRange(GetToiletInfo(s.UniqueId, toilets));
                    info.Infos.Add(new(StopPointInfoKey.WiFi,
                        s.Wifi.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? "Y" : "N"));

                    if (!s.FareZones.Equals("outside", StringComparison.CurrentCultureIgnoreCase))
                    {
                        info.Infos.Add(new(StopPointInfoKey.TfLZone, s.FareZones.Replace("|", "/")));
                    }

                    info.Infos.Add(new(StopPointInfoKey.BlueBadgeParking,
                        s.BlueBadgeCarParking.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? "Y" : "N"));
                    info.Infos.Add(new(StopPointInfoKey.AccessibilityViaLift,
                        lifts.Any(l => l.StationUniqueId == s.UniqueId) ? "Y" : "N"));
                    return info;
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading");
        }

        return results;
    }

    private async Task<ICollection<tfl_StopPoint>> StopPointsForTypes(IEnumerable<string> types,
        ICollection<string> allowedModes)
    {
        var stopPoints = new List<tfl_StopPoint>();
        try
        {
            var page = 1;
            var typeString = string.Join(',', types);
            var reachedEnd = false;

            while (!reachedEnd)
            {
                using var response = await _api.GetAsync($"StopPoint/Type/{typeString}/page/{page}",
                    HttpCompletionOption.ResponseHeadersRead);
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
                    result.children = result.children.Where(c =>
                        c.lineModeGroups.Any() && string.IsNullOrWhiteSpace(c.stopType) || types.Contains(c.stopType));
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

    private static async Task<List<T>> DecodeCsv<T>(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecordsAsync<T>();
        return await records.ToListAsync();
    }

    private static List<KeyValuePair<StopPointInfoKey, string>> GetToiletInfo(string stopId, IEnumerable<tfl_lrad_ToiletInfo> lrad)
    {
        var relevant = lrad.Where(l => l.StationUniqueId == stopId).ToList();
        if (relevant.Count == 0)
        {
            return [];
        }

        var infos = new List<KeyValuePair<StopPointInfoKey, string>>();

        infos.Add(new(StopPointInfoKey.Toilets, "Y"));

        var maleToilets = relevant.Where(t => t.Type.Equals("male", StringComparison.OrdinalIgnoreCase)).ToList();
        var womenToilets = relevant.Where(t => t.Type.Equals("female", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var unisexToilets = relevant.Where(t => t.Type.Equals("unisex", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (maleToilets.Count != 0)
        {
            var hasAccessible = maleToilets.Any(t => t.IsAccessible.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasBaby = maleToilets.Any(t => t.HasBabyChanging.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasFree = maleToilets.Any(t => t.IsFeeCharged.Equals("false", StringComparison.OrdinalIgnoreCase));
            var notes = string.Join('\n', maleToilets.Select(t => t.Location));

            infos.Add(new(StopPointInfoKey.ToiletsMen, "Y"));
            infos.Add(new(StopPointInfoKey.ToiletsMenAccessible, hasAccessible ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsMenBaby, hasBaby ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsMenFree, hasFree ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsMenNote, notes));
        }
        else
        {
            infos.Add(new(StopPointInfoKey.ToiletsMen, "N"));
        }

        if (womenToilets.Count != 0)
        {
            var hasAccessible = womenToilets.Any(t => t.IsAccessible.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasBaby = womenToilets.Any(t => t.HasBabyChanging.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasFree = womenToilets.Any(t => t.IsFeeCharged.Equals("false", StringComparison.OrdinalIgnoreCase));
            var notes = string.Join('\n', womenToilets.Select(t => t.Location));

            infos.Add(new(StopPointInfoKey.ToiletsWomen, "Y"));
            infos.Add(new(StopPointInfoKey.ToiletsWomenAccessible, hasAccessible ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsWomenBaby, hasBaby ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsWomenFree, hasFree ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsWomenNote, notes));
        }
        else
        {
            infos.Add(new(StopPointInfoKey.ToiletsWomen, "N"));
        }

        if (unisexToilets.Count != 0)
        {
            var hasAccessible = unisexToilets.Any(t => t.IsAccessible.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasBaby = unisexToilets.Any(t => t.HasBabyChanging.Equals("true", StringComparison.OrdinalIgnoreCase));
            var hasFree = unisexToilets.Any(t => t.IsFeeCharged.Equals("false", StringComparison.OrdinalIgnoreCase));
            var notes = string.Join('\n', unisexToilets.Select(t => t.Location));

            infos.Add(new(StopPointInfoKey.ToiletsUnisex, "Y"));
            infos.Add(new(StopPointInfoKey.ToiletsUnisexAccessible, hasAccessible ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsUnisexBaby, hasBaby ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsUnisexFree, hasFree ? "Y" : "N"));
            infos.Add(new(StopPointInfoKey.ToiletsUnisexNote, notes));
        }
        else
        {
            infos.Add(new(StopPointInfoKey.ToiletsUnisex, "N"));
        }

        return infos;
    }
}