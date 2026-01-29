using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackii.Models.Admin.Subfamily;
using Trackii.Services.Admin;

namespace Trackii.Controllers.Admin;

[Authorize]
[Route("Admin/[controller]")]
public class SubfamilyController : Controller
{
    private readonly SubfamilyService _svc;

    public SubfamilyController(SubfamilyService svc)
    {
        _svc = svc;
    }

    // ===================== INDEX =====================
    [HttpGet("")]
    public IActionResult Index(
        uint? areaId,
        uint? familyId,
        string? search,
        bool showInactive = false,
        int page = 1)
    {
        var vm = _svc.GetPaged(areaId, familyId, search, showInactive, page, 10);
        return View(vm);
    }

    // ===================== CREATE =====================
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewBag.Families = _svc.GetActiveFamilies();
        return View(new SubfamilyEditVm());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(SubfamilyEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Families = _svc.GetActiveFamilies();
            return View(vm);
        }

        _svc.Create(vm);
        return RedirectToAction(nameof(Index));
    }

    // ===================== EDIT =====================
    [HttpGet("Edit/{id:long}")]
    public IActionResult Edit(uint id)
    {
        var vm = _svc.GetById(id);
        if (vm == null) return NotFound();

        ViewBag.Families = _svc.GetActiveFamilies();
        return View(vm);
    }

    [HttpPost("Edit/{id:long}")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(SubfamilyEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Families = _svc.GetActiveFamilies();
            return View(vm);
        }

        _svc.Update(vm);
        return RedirectToAction(nameof(Index));
    }

    // ===================== TOGGLE =====================
    [HttpPost("Toggle/{id:long}")]
    [ValidateAntiForgeryToken]
    public IActionResult Toggle(uint id)
    {
        _svc.SetActive(id, true); // el service valida padre activo
        return RedirectToAction(nameof(Index));
    }
}
