using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Models.Api;
using Trackii.Services.Api;

namespace Trackii.Controllers.Api;

[ApiController]
[Route("api/v1/device")]
[Authorize(AuthenticationSchemes = "ApiBearer")]
public class DeviceApiController : ControllerBase
{
    private readonly DeviceApiService _svc;

    public DeviceApiController(DeviceApiService svc)
    {
        _svc = svc;
    }

    [HttpPost("bind")]
    public ActionResult<DeviceBindResponse> Bind([FromBody] DeviceBindRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceUid))
            return BadRequest("deviceUid requerido.");

        var (deviceId, locId, locName) = _svc.Bind(req.DeviceUid.Trim(), req.LocationId);

        return Ok(new DeviceBindResponse
        {
            DeviceId = deviceId,
            LocationId = locId,
            LocationName = locName
        });
    }
}
