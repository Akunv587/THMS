using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Models;

namespace EquipmentIMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService, ICurrentUserService currentUser)
    {
        _logger = logger;
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
        var isManager = _currentUser.IsInRole("Manager") || _currentUser.IsInRole("Admin");
        var data = await _dashboardService.GetDashboardDataAsync(_currentUser.UserId, isManager);
        return Json(ApiResponse.Ok(data));
    }

    [HttpGet]
    public async Task<IActionResult> GetSaleTrend()
    {
        var data = await _dashboardService.GetSaleTrendAsync();
        return Json(ApiResponse.Ok(data));
    }

    [HttpGet]
    public async Task<IActionResult> GetStockDist()
    {
        var data = await _dashboardService.GetStockDistAsync();
        return Json(ApiResponse.Ok(data));
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingApprovals()
    {
        var data = await _dashboardService.GetPendingApprovalsAsync();
        return Json(ApiResponse.Ok(data));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
