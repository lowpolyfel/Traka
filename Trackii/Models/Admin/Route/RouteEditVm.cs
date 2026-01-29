namespace Trackii.Models.Admin.Route;

public class RouteEditVm
{
    public uint Id { get; set; }

    public uint SubfamilyId { get; set; }

    public string Name { get; set; } = string.Empty;

    // Se genera automáticamente por subfamilia (100, 200, 300...)
    public string Version { get; set; } = string.Empty;

    // Lista ordenada (solo seleccionados)
    public List<RouteStepVm> Steps { get; set; } = new();

    public string? Error { get; set; }
}
