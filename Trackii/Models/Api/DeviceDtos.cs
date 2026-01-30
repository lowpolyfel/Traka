namespace Trackii.Models.Api;

public class DeviceBindRequest
{
    public string DeviceUid { get; set; } = "";
    public uint LocationId { get; set; }
}

public class DeviceBindResponse
{
    public uint DeviceId { get; set; }
    public uint LocationId { get; set; }
    public string LocationName { get; set; } = "";
}
