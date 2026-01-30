namespace Trackii.Models.Api;

public class ScanRequest
{
    public uint DeviceId { get; set; }

    // Code128 escaneado por la tablet
    public string Barcode { get; set; } = "";

    public uint Qty { get; set; }
}



public class ScanResponse
{
    public bool Ok { get; set; }
    public bool Advanced { get; set; }
    public string Status { get; set; } = ""; // ACTIVE / FINISHED / SCRAPPED
    public string Reason { get; set; } = ""; // STEP_MISMATCH / QTY_GREATER_THAN_PREVIOUS / NOT_FOUND / etc

    public uint? CurrentStep { get; set; }
    public string? ExpectedLocation { get; set; }

    public uint? QtyIn { get; set; }
    public uint? PreviousQty { get; set; }
    public uint? Scrap { get; set; }

    public uint? NextStep { get; set; }
    public string? NextLocation { get; set; }
}
