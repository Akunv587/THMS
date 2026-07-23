using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;

namespace EquipmentIMS.Web.Controllers;

[Route("[controller]")]
[PermissionFilter("sys:backup:view")]
public class BackupController : Controller
{
    private readonly IBackupService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;

    public BackupController(IBackupService service, ICurrentUserService currentUser, IWebHostEnvironment env)
    {
        _service = service;
        _currentUser = currentUser;
        _env = env;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var history = await _service.GetBackupHistoryAsync(page, pageSize);
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        return View(history);
    }

    [HttpPost("Backup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Backup()
    {
        var userId = _currentUser.UserId.ToString();
        var userName = _currentUser.RealName;
        var result = await _service.BackupAsync(userId, userName);

        if (result.Success)
            TempData["Message"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Verify/{id}")]
    [ActionName("Verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(long id)
    {
        var ok = await _service.VerifyBackupAsync(id);
        TempData["Message"] = ok ? "备份文件验证通过" : "备份文件验证失败";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Download/{id}")]
    [ActionName("Download")]
    public async Task<IActionResult> Download(long id)
    {
        var filePath = await _service.GetBackupFilePathAsync(id);
        if (filePath == null || !System.IO.File.Exists(filePath))
        {
            TempData["Error"] = "备份文件不存在或已被清理";
            return RedirectToAction(nameof(Index));
        }

        var fileName = Path.GetFileName(filePath);
        var contentType = "application/octet-stream";
        return PhysicalFile(filePath, contentType, fileName);
    }
}
