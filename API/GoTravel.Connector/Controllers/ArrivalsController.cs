using System.Net;
using GoTravel.Connector.Services.Interfaces;
using GoTravel.Standard.Models.Arrivals;
using Microsoft.AspNetCore.Mvc;

namespace GoTravel.Connector.Controllers;

[ApiController]
[Route("[controller]")]
public class ArrivalsController: ControllerBase
{
    private IConnectorGeneralArrivalService _arrivals;

    public ArrivalsController(IConnectorGeneralArrivalService arrivals)
    {
        _arrivals = arrivals;
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
            //TODO: Log
            return StatusCode(500);
        }
    }
    
}