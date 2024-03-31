using System.Net;
using System.Text.Json;
using GoTravel.Connector.Connections.Exceptions;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Standard.Models.MessageModels;
using Polly;
using Polly.Retry;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TfLLineService(IHttpClientFactory api) : IGenericLinesService, ITfLLineService
{
    private const string OperatorPrefix = "tfl-";
    private readonly HttpClient _api = api.CreateClient("TfLAPI");
    
    public async Task<ICollection<tfl_LineMode>> RetrieveAllLineModes(CancellationToken ct = default)
    {
        var result = await _api.GetStreamAsync("Line/Meta/Modes", ct);
        var modes = await JsonSerializer.DeserializeAsync<ICollection<tfl_LineMode>>(result, cancellationToken: ct);

        if (modes is null)
        {
            throw new NoLineModesException("TfL");
        }
        
        return modes;
    }

    public async Task<ICollection<tfl_Line>> RetrieveAllLinesForMode(string lineMode, CancellationToken ct = default)
    {
        var result = await _api.GetStreamAsync($"Line/Mode/{lineMode}", ct);
        var lines = await JsonSerializer.DeserializeAsync<ICollection<tfl_Line>>(result, cancellationToken: ct);

        if (lines is null)
        {
            throw new NoLinesException("TfL", lineMode);
        }
        
        return lines;
    }

    public async Task<ICollection<LineModeUpdateDto>> GetLineModeDtos(CancellationToken ct = default)
    {
        var lineModes = await RetrieveAllLineModes(ct);
        
        var tasks = lineModes
            .Where(m => m.isTflService)
            .Select(x => RetrieveAllLinesForMode(x.modeName, ct));
        var groups = await Task.WhenAll(tasks);

        var dtos= groups
            .Select(lineGroup => new LineModeUpdateDto
                { 
                    Lines = lineGroup.Select(x => OperatorPrefix + x.id).ToList(),
                    LineModeName = OperatorPrefix + lineGroup.FirstOrDefault()?.modeName ?? ""
                }
            )
            .Where(g => g.LineModeName != OperatorPrefix)
            .ToList();

        return dtos;
    }
    
    public async Task<ICollection<LineStringUpdateDto>> GetLineRouteDtos(CancellationToken ct = default)
    {
        var tflLineRoutes = await RetrieveLineStrings(ct);

        var dtos = tflLineRoutes
            .Where(r => r is not null)
            .SelectMany(r =>
            {
                var routes = new List<LineStringUpdateDto>();
                for (var i = 0; i < r.lineStrings.Count; i++)
                {
                    var olr = r.orderedLineRoutes.ElementAtOrDefault(i);
                    routes.Add(new LineStringUpdateDto
                    {
                        LineId = OperatorPrefix + r.lineId,
                        Direction = r.direction,
                        Name = olr?.name ?? "",
                        Service = olr?.serviceType ?? "Regular",
                        Route = JsonSerializer.Deserialize<ICollection<ICollection<ICollection<double>>>>(r.lineStrings.ElementAt(i)) ?? new List<ICollection<ICollection<double>>>()
                    });
                }

                return routes;
            });

        return dtos.ToList();
    }
    
    public async Task<ICollection<tfl_LineStringResponse?>> RetrieveLineStrings(CancellationToken ct = default)
    {
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        
        var lineModes = (await RetrieveAllLineModes(ct))
            .Where(m => m is { isTflService: true, isScheduledService: true})
            .Select(m => m.modeName);

        const int maxConcurrentTasks = 50;
        var semaphore = new SemaphoreSlim(maxConcurrentTasks);
        
        var modeRouteSectionResponse = await _api.GetStreamAsync($"Line/Mode/{string.Join(',', lineModes)}/Route", ct);
        var modeRouteSections = await JsonSerializer.DeserializeAsync<ICollection<tfl_LineModeRoutes>>(modeRouteSectionResponse, cancellationToken: ct);

        var tasks = modeRouteSections.SelectMany(m => m.routeSections.Select(async r =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var response = await retryPolicy.ExecuteAsync(() => _api.GetAsync($"Line/{m.id}/route/sequence/{r.direction}", HttpCompletionOption.ResponseHeadersRead, ct));
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(ct);
                    return await JsonSerializer.DeserializeAsync<tfl_LineStringResponse>(stream, cancellationToken: ct);
                }

                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }));

        var routes = await Task.WhenAll(tasks);
        return routes.ToList();
    }
}