using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Web.ViewModels;

namespace EquipmentIMS.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemConfigService _configService;
    private readonly ICacheService _cache;
    private readonly IUserService _userService;
    private readonly PasswordHasher<SysUser> _passwordHasher = new();

    private const int DefaultMaxLoginAttempts = 5;
    private const int DefaultPasswordHistoryCount = 5;
    private const int LockoutMinutes = 30;

    public AccountController(
        AppDbContext db,
        ICurrentUserService currentUser,
        ISystemConfigService configService,
        ICacheService cache,
        IUserService userService)
    {
        _db = db;
        _currentUser = currentUser;
        _configService = configService;
        _cache = cache;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // 检查锁定（容错：异常不影响登录流程）
        try
        {
            var lockInfo = await GetLoginLockAsync(model.UserName);
            if (lockInfo != null && lockInfo.LockUntil > DateTime.Now)
            {
                ModelState.AddModelError(string.Empty,
                    $"账户已被锁定，请在 {lockInfo.LockUntil:HH:mm:ss} 后重试");
                return View(model);
            }
        }
        catch
        {
            // 锁定检查异常不影响登录流程
        }

        var user = await _db.SysUsers
            .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.IsActive && !u.IsDeleted);

        if (user == null)
        {
            await IncrementLoginFailSafeAsync(model.UserName, null);
            ModelState.AddModelError(string.Empty, "用户名或密码错误");
            return View(model);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            await IncrementLoginFailSafeAsync(model.UserName, null);
            ModelState.AddModelError(string.Empty, "用户名或密码错误");
            return View(model);
        }

        // 密码需要升级哈希
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        // 登录成功，清除锁定记录
        try { await _cache.RemoveAsync($"LoginLock:{model.UserName}"); } catch { }

        // 更新登录信息
        user.LastLoginTime = DateTime.Now;
        user.LastLoginIP = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _db.SaveChangesAsync();

        // 创建 Claims
        var roles = await _db.SysUserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.SysRoles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RoleCode)
            .ToListAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("RealName", user.RealName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        // 强制修改密码
        if (user.MustChangePwd)
            return RedirectToAction(nameof(ChangePassword));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _currentUser.UserId;
        if (userId <= 0)
            return RedirectToAction(nameof(Login));

        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            return RedirectToAction(nameof(Login));

        // 1. 验证原密码
        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.OldPassword), "原密码错误");
            return View(model);
        }

        // 2. 密码强度校验 (C.3)
        if (!IsPasswordStrong(model.NewPassword))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "密码需至少8位，包含大小写字母和数字");
            return View(model);
        }

        // 3. 密码历史检查 (C.3) — 从系统配置动态读取保留数量
        var historyCountStr = await _configService.GetAsync("PasswordHistoryCount");
        var historyCount = int.TryParse(historyCountStr, out var hc) && hc > 0 ? hc : DefaultPasswordHistoryCount;

        var passwordHistories = await _userService.GetPasswordHistoriesAsync(userId, historyCount);
        foreach (var history in passwordHistories)
        {
            var historyResult = _passwordHasher.VerifyHashedPassword(user, history.PasswordHash, model.NewPassword);
            if (historyResult != PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.NewPassword), $"不能使用最近 {historyCount} 次使用过的密码");
                return View(model);
            }
        }

        // 4. 保存旧密码到历史
        await _userService.AddPasswordHistoryAsync(new SysPasswordHistory
        {
            UserId = userId,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            CreateTime = DateTime.Now
        });

        // 5. 更新密码
        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        user.PasswordSalt = string.Empty;
        user.MustChangePwd = false;
        user.LastPasswordChanged = DateTime.Now;

        await _db.SaveChangesAsync();

        // 6. 清理超出的密码历史记录 (C.3)
        await _userService.TrimPasswordHistoryAsync(userId, historyCount);

        TempData["Message"] = "密码修改成功";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    #region 辅助方法

    /// <summary>C.3: 密码强度校验 — 至少8位，含大小写字母和数字</summary>
    private static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        return hasUpper && hasLower && hasDigit;
    }

    #endregion

    #region 登录锁定辅助方法

    private class LoginLockInfo
    {
        public int FailCount { get; set; }
        public DateTime LockUntil { get; set; }
    }

    private async Task<LoginLockInfo?> GetLoginLockAsync(string userName)
    {
        return await _cache.GetAsync<LoginLockInfo>($"LoginLock:{userName}");
    }

    /// <summary>C.3: 登录失败计数 — 容错包装，异常不影响登录流程</summary>
    private async Task IncrementLoginFailSafeAsync(string userName, LoginLockInfo? existing)
    {
        try
        {
            var info = existing ?? await GetLoginLockAsync(userName);
            await IncrementLoginFailAsync(userName, info);
        }
        catch
        {
            // 锁定记录失败不影响登录流程
        }
    }

    private async Task IncrementLoginFailAsync(string userName, LoginLockInfo? existing)
    {
        var maxAttemptsStr = await _configService.GetAsync("MaxLoginAttempts");
        var maxAttempts = int.TryParse(maxAttemptsStr, out var val) && val > 0 ? val : DefaultMaxLoginAttempts;

        var info = existing ?? new LoginLockInfo { FailCount = 0, LockUntil = DateTime.MinValue };

        info.FailCount++;

        if (info.FailCount >= maxAttempts)
        {
            info.LockUntil = DateTime.Now.AddMinutes(LockoutMinutes);
        }

        await _cache.SetAsync($"LoginLock:{userName}", info, TimeSpan.FromHours(1));
    }

    #endregion
}
