using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Standard.Models.Journeys;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TflJourneyService: IGenericJourneyService, ITflJourneyService
{
    private const string OperatorPrefix = "tfl-";
    private readonly HashSet<string> OperatorExcludedModes = new(StringComparer.OrdinalIgnoreCase) { "walking", "cycle" };
    private HttpClient _client;

    public TflJourneyService(IHttpClientFactory httpFactory)
    {
        _client = httpFactory.CreateClient("TfLAPI");
    }
    
    
    public async Task<ICollection<Journey>> GetJourneyOptionDtos(JourneyRequest request, CancellationToken ct = default)
    {
        var tflJourneys = await GetTflJourneys(request, ct);

        var dtos = tflJourneys.Select(j => new Journey
        {
            TotalDuration = Convert.ToInt32(j.duration),
            BeginJourneyAt = j.startDateTime,
            EndJourneyAt = j.arrivalDateTime,
            
            JourneyLegs = j.legs.Select(l => new JourneyLeg
            {
                BeginLegAt = l.departureTime ?? l.scheduledDepartureTime,
                EndLegAt = l.arrivalTime ?? l.scheduledArrivalTime,
                LegDuration = Convert.ToInt32(l.duration),
                StartAtStopId = l.departurePoint?.naptanId,
                StartAtStopName = l.departurePoint?.commonName,
                EndAtStopId = l.arrivalPoint?.naptanId,
                EndAtStopName = l.arrivalPoint?.commonName,
                LegDetails = MapLegDetails(l)
            }).ToList(),
        });



        return dtos.ToList();
    }

    public async Task<ICollection<tfl_Journey>> GetTflJourneys(JourneyRequest request, CancellationToken ct = default)
    {
        var from = $"{request.StartPoint.Lat},{request.StartPoint.Lon}";
        var to = $"{request.EndPoint.Lat},{request.EndPoint.Lon}";

        var url = $"Journey/JourneyResults/{from}/to/{to}";
        var queryParams = new NameValueCollection();
        queryParams.Add("nationalSearch", "false");
        if (request.JourneyTime is not null)
        {
            queryParams.Add("date", request.JourneyTime.Value.ToString("yyyyMMdd"));
            queryParams.Add("time", request.JourneyTime.Value.ToString("HHmm"));
            queryParams.Add("timeIs", request.JourneyTimeIsDeparture ? "departing": "arriving");
        }

        if (request.PreferenceMode is not null)
        {
            queryParams.Add("journeyPreference", TflJourneyPreference(request.PreferenceMode.Value));
        }

        if (request.AccessibilityPreferences is not null)
        {
            queryParams.Add("accessibilityPreference", string.Join(",", request.AccessibilityPreferences.Select(TflAccessibilityPreference)));
        }

        if (request.ViaLocation is not null)
        {
            queryParams.Add("via", $"{request.ViaLocation.Lat},{request.ViaLocation.Lon}");
        }
        else
        {
            //TODO: Replace this and always call if TfL respond around issues
            // https://techforum.tfl.gov.uk/t/journey-api-usemultimodal-ignores-via-location/3145
            queryParams.Add("useMultiModalCall", "true");
        }
        
        queryParams.Add("routeBetweenEntrances", "true");

        var queryString = string.Join("&", queryParams.AllKeys.Select(k => k + "=" + queryParams[k]));
        
        await using var result = await _client.GetStreamAsync(url + "?" + queryString, ct);
        var journey = await JsonSerializer.DeserializeAsync<tfl_JourneyResult>(result, cancellationToken: ct);

        return journey?.journeys ?? new List<tfl_Journey>();
    }

    private JourneyLegDetails MapLegDetails(tfl_JourneyLeg leg)
    {
        var dto = new JourneyLegDetails();

        dto.Summary = leg.instruction.summary;
        dto.DetailedSummary = leg.instruction.detailed;
        dto.ModeId = OperatorExcludedModes.Contains(leg.mode.id) ? leg.mode.id : OperatorPrefix + leg.mode.id; 
        dto.LineIds = leg.routeOptions.Where(o => o.lineIdentifier is not null).Select(o => OperatorPrefix + o.lineIdentifier?.id).ToList();
        dto.LegSteps = new List<JourneyLegStep>();
        foreach (var step in leg.instruction.steps)
        {
            dto.LegSteps.Add(new JourneyLegStep
            {
                Summary = step.descriptionHeading + " " + step.description,
                Direction = step.skyDirectionDescription,
                Latitude = step.latitude,
                Longitude = step.longitude,
                DistanceOfStep = Convert.ToInt32(step.distance),
            });
        }

        return dto;
    }

    private string TflJourneyPreference(JourneyPreferenceMode mode)
    {
        return mode switch
        {
            JourneyPreferenceMode.LeastChanges => "leastinterchange",
            JourneyPreferenceMode.LeastTime => "leasttime",
            JourneyPreferenceMode.LeastWalking => "leastwalking",
            _ => ""
        };
    }

    private string TflAccessibilityPreference(JourneyAccessibilityPreference preference)
    {
        return preference switch
        {
            JourneyAccessibilityPreference.NoElevators => "noElevators",
            JourneyAccessibilityPreference.NoStairs => "noSolidStairs",
            JourneyAccessibilityPreference.NoEscalators => "noEscalators",
            JourneyAccessibilityPreference.StepFreeToVehicle => "stepFreeToVehicle",
            JourneyAccessibilityPreference.StepFreeToPlatform => "stepFreeToPlatform",
            _ => ""
        };
    }
}