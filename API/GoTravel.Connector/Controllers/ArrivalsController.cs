using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Models.Arrivals;
using Microsoft.AspNetCore.Mvc;

namespace GoTravel.Connector.Controllers;

[ApiController]
[Route("[controller]")]
public class ArrivalsController: ControllerBase
{
    private IConnectorGeneralArrivalService _arrivals;
    private readonly ILogger<ArrivalsController> _log;

    public ArrivalsController(IConnectorGeneralArrivalService arrivals, ILogger<ArrivalsController> log)
    {
        _arrivals = arrivals;
        _log = log;
    }

    [HttpGet]
    [Route("{stopPointId}")]
    [Produces(typeof(ICollection<ArrivalDeparture>))]
    [ProducesResponseType(typeof(StatusCodeResult), 500)]
    public async Task<IActionResult> GetArrivalsForStop(string stopPointId, CancellationToken ct = default)
    {
        try
        {
            var arrivals = await _arrivals.GetArrivalsForStopAsync(stopPointId, ct);
            return Ok(arrivals);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to retrieve arrivals for {Stop}", stopPointId);
            return StatusCode(500);
        }
    }
}