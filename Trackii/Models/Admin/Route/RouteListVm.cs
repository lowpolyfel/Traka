namespace Trackii.Models.Admin.Route;

public class RouteListVm
{
    public uint? SubfamilyId { get; set; }
    public string? Search { get; set; }
    public bool ShowInactive { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int Total { get; set; }
    public int TotalPages { get; set; }

    public List<RouteRowVm> Items { get; set; } = new();
}
