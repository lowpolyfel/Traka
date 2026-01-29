using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Models.Admin.Route;
using Trackii.Services.Admin;

namespace Trackii.Controllers.Admin;

[Authorize]
[Route("Admin/[controller]")]
public class RouteController : Controller
{
    private readonly RouteService _service;

    public RouteController(RouteService service)
    {
        _service = service;
    }

    private void LoadLookups(uint? selectedSubfamilyId = null)
    {
        ViewBag.Subfamilies = _service.GetActiveSubfamilies();
        ViewBag.Locations = _service.GetActiveLocations();
        ViewBag.SelectedSubfamilyId = selectedSubfamilyId;
    }

    [HttpGet("")]
    public IActionResult Index(uint? subfamilyId, string? search, bool showInactive = false, int page = 1, int pageSize = 10)
    {
        LoadLookups(subfamilyId);
        var vm = _service.GetPaged(subfamilyId, search, showInactive, page, pageSize);
        return View(vm);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        LoadLookups(null);
        return View(_service.GetForCreate());
    }

    [HttpGet("Edit/{id}")]
    public IActionResult Edit(uint id)
    {
        var vm = _service.GetForEdit(id);
        LoadLookups(vm.SubfamilyId);
        return View(vm);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(RouteEditVm vm)
    {
        try
        {
            _service.Save(vm);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            vm.Error = ex.Message;
            LoadLookups(vm.SubfamilyId);
            return View(vm.Id == 0 ? "Create" : "Edit", vm);
        }
    }

    [HttpPost("Activate/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Activate(uint id)
    {
        try
        {
            _service.Activate(id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
