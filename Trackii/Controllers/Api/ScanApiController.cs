using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Models.Api;
using Trackii.Services.Api;

namespace Trackii.Controllers.Api;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = "ApiBearer")]
public class ScanApiController : ControllerBase
{
    private readonly ScanApiService _scan;

    public ScanApiController(ScanApiService scan)
    {
        _scan = scan;
    }

    [HttpPost("scan")]
    public ActionResult<ScanResponse> Scan([FromBody] ScanRequest req)
    {
        // 🔒 DEFINITIVO: barcode obligatorio
        if (string.IsNullOrWhiteSpace(req.Barcode))
            return BadRequest("barcode requerido.");

        var uid = User.FindFirstValue("uid");
        if (string.IsNullOrWhiteSpace(uid) || !uint.TryParse(uid, out var userId))
            return Unauthorized("Token sin uid.");

        // 🔒 El WO SIEMPRE sale del barcode
        // Ej: WO-TEST-001-0001 → WO-TEST-001
        var parts = req.Barcode.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var woNumber = parts[0];

        var r = _scan.Scan(userId, req.DeviceId, woNumber, req.Qty);

        return Ok(new ScanResponse
        {
            Ok = r.Ok,
            Advanced = r.Advanced,
            Status = r.Status,
            Reason = r.Reason,
            CurrentStep = r.CurrentStep,
            ExpectedLocation = r.ExpectedLocation,
            QtyIn = r.QtyIn,
            PreviousQty = r.PreviousQty,
            Scrap = r.Scrap,
            NextStep = r.NextStep,
            NextLocation = r.NextLocation
        });
    }
}
