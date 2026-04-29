using Korendzh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Korendzh.Web.Controllers;

[ApiController]
[Route("api/cars")]
[Authorize]
public class CarsController : ControllerBase
{
    private readonly ICarService _cars;

    public CarsController(ICarService cars)
    {
        _cars = cars;
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string? q, CancellationToken ct)
    {
        var items = await _cars.SearchAsync(q, take: 15, ct);
        return Ok(items.Select(c => new { id = c.Id, name = c.Name, licensePlate = c.LicensePlate }));
    }
}
