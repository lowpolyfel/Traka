namespace Trackii.Models.Admin.Route;

public class RouteStepVm
{
    public uint LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;

    // En POST lo usamos para el orden.
    public int StepNumber { get; set; }
}
