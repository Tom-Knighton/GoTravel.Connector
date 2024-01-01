using System.Text.Json;
using GoTravel.Connector.Connections.Exceptions;
using GoTravel.Connector.Connections.TfL.Interfaces;
using GoTravel.Connector.Connections.TfL.Models;
using GoTravel.Connector.Domain.Interfaces;
using GoTravel.Standard.Models.MessageModels;

namespace GoTravel.Connector.Connections.TfL.Services;

public class TfLLineService: IGenericLinesService, ITfLLineService
{
    private const string OperatorPrefix = "tfl-";
    private readonly HttpClient _api;

    public TfLLineService(IHttpClientFactory api)
    {
        _api = api.CreateClient("TfLAPI");
    }
    
    public async Task<ICollection<tfl_LineMode>> RetrieveAllLineModes()
    {
        var result = await _api.GetStreamAsync("Line/Meta/Modes");
        var modes = await JsonSerializer.DeserializeAsync<ICollection<tfl_LineMode>>(result);

        if (modes is null)
        {
            throw new NoLineModesException("TfL");
        }
        
        return modes;
    }

    public async Task<ICollection<tfl_Line>> RetrieveAllLinesForMode(string lineMode)
    {
        var result = await _api.GetStreamAsync($"Line/Mode/{lineMode}");
        var lines = await JsonSerializer.DeserializeAsync<ICollection<tfl_Line>>(result);

        if (lines is null)
        {
            throw new NoLinesException("TfL", lineMode);
        }
        
        return lines;
    }

    public async Task<ICollection<LineModeUpdateDto>> GetLineModeDtos(CancellationToken ct)
    {
        var lineModes = await RetrieveAllLineModes();
        
        var tasks = lineModes
            .Where(m => m.isTflService)
            .Select(x => RetrieveAllLinesForMode(x.modeName));
        var groups = await Task.WhenAll(tasks);

        var dtos= groups
            .Select(lineGroup => new LineModeUpdateDto
                { 
                    Lines = lineGroup.Select(x => OperatorPrefix + x.name).ToList(),
                    LineModeName = OperatorPrefix + lineGroup.FirstOrDefault()?.modeName ?? ""
                }
            )
            .Where(g => !string.IsNullOrEmpty(g.LineModeName))
            .ToList();

        return dtos;
    }
}