using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Services.Api;

namespace Trackii.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/v1/location")]
public class LocationApiController : ControllerBase
{
    private readonly LocationListApiService _svc;

    public LocationApiController(LocationListApiService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_svc.GetAll());
    }
}
