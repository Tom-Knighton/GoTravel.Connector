using GoTravel.Connector.Domain.Exceptions;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Models.Journeys;
using Microsoft.AspNetCore.Mvc;

namespace GoTravel.Connector.Controllers;

[ApiController]
[Route("[controller]")]
public class JourneyController: ControllerBase
{

    private IConnectorJourneyService _journeyService;

    public JourneyController(IConnectorJourneyService service)
    {
        _journeyService = service;
    }

    [HttpPost]
    [Route("Options")]
    public async Task<IActionResult> GetJourneyResults([FromBody] JourneyRequest request, [FromQuery] List<string>? excludeOperator = null, CancellationToken ct = default)
    {
        try
        {
            var journeys = await _journeyService.GetPossibleJourneys(request, excludeOperator?.ToHashSet(), ct);

            return Ok(journeys);
        }
        catch (ConnectionDoesntExistException ex)
        {
            return NotFound("Connection doesn't exist");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}