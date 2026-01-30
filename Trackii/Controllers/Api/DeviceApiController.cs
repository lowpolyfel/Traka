using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Models.Api;
using Trackii.Services.Api;

namespace Trackii.Controllers.Api;

[ApiController]
[Route("api/v1/device")]
[AllowAnonymous]
public class DeviceActivationApiController : ControllerBase
{
    private readonly DeviceActivationApiService _svc;

    public DeviceActivationApiController(DeviceActivationApiService svc)
    {
        _svc = svc;
    }

    [HttpPost("activate")]
    public ActionResult<DeviceActivationResponse> Activate(
        [FromBody] DeviceActivationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest("token requerido");

        if (string.IsNullOrWhiteSpace(req.AndroidId))
            return BadRequest("androidId requerido");

        var result = _svc.Activate(req.Token.Trim(), req.AndroidId.Trim());

        if (!result.Ok)
            return Unauthorized(result.Reason);

        return Ok(result);
    }
}
