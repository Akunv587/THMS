using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.Web.Filters;

public class OperationLogActionFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly IOperationLogRepository _logRepo;

    public OperationLogActionFilter(ICurrentUserService currentUser, IOperationLogRepository logRepo)
    {
        _currentUser = currentUser;
        _logRepo = logRepo;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var log = new SysOperationLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            Module = GetModule(context),
            Action = GetAction(context),
            IP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            CreateTime = DateTime.Now
        };

        // 记录请求参数作为 NewValue（POST 请求的新值）
        log.NewValue = SerializeArgs(context.ActionArguments);

        // 执行 Action
        var executedContext = await next();

        // 从结果中提取 TargetId（如返回的实体 Id）
        if (executedContext.Result is ObjectResult objResult && objResult.Value != null)
        {
            log.TargetType = objResult.Value.GetType().Name;
            var idProp = objResult.Value.GetType().GetProperty("Id");
            if (idProp != null)
            {
                log.TargetId = idProp.GetValue(objResult.Value) as long?;
                log.Description = $"操作 {log.TargetType}:{log.TargetId}";
            }
        }

        // 异步写入日志，不阻塞主流程
        _ = Task.Run(async () =>
        {
            try { await _logRepo.AddAsync(log); }
            catch { /* 日志写入失败不影响主流程 */ }
        });
    }

    private static string GetModule(ActionExecutingContext context)
    {
        var controller = context.Controller.GetType().Name;
        return controller.Replace("Controller", "");
    }

    private static string GetAction(ActionExecutingContext context)
    {
        var routeData = context.RouteData;
        return routeData.Values["action"]?.ToString() ?? "Unknown";
    }

    private static string? SerializeArgs(IDictionary<string, object?> args)
    {
        if (args.Count == 0) return null;
        try
        {
            var filtered = new Dictionary<string, object?>();
            foreach (var kv in args)
            {
                if (kv.Key is "Password" or "OldPassword" or "NewPassword" or "ConfirmPassword")
                    filtered[kv.Key] = "***";
                else
                    filtered[kv.Key] = kv.Value;
            }
            return JsonSerializer.Serialize(filtered);
        }
        catch
        {
            return null;
        }
    }
}
