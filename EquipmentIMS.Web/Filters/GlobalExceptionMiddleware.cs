using System.Net;
using System.Text.Json;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Exceptions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EquipmentIMS.Web.Filters;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly TelemetryClient? _telemetryClient;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, TelemetryClient? telemetryClient = null)
    {
        _next = next;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "业务异常: {ErrorCode} {Message}", ex.ErrorCode, ex.Message);
            await WriteJsonResponse(context, HttpStatusCode.BadRequest,
                ApiResponse.Fail(ex.Message, ex.ErrorCode));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "参数校验失败");
            await WriteJsonResponse(context, HttpStatusCode.BadRequest,
                ApiResponse.FailValidation(ex.Errors));
        }
        catch (UnauthorizedAccessException)
        {
            if (IsAjaxRequest(context))
            {
                await WriteJsonResponse(context, HttpStatusCode.Forbidden,
                    ApiResponse.Fail("访问被拒绝", "FORBIDDEN"));
            }
            else
            {
                context.Response.Redirect("/Account/AccessDenied");
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "并发冲突");
            await WriteJsonResponse(context, HttpStatusCode.Conflict,
                ApiResponse.Fail("数据已被他人修改，请刷新后重试", "STOCK_CONFLICT"));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "资源未找到: {Message}", ex.Message);
            await WriteJsonResponse(context, HttpStatusCode.NotFound,
                ApiResponse.Fail(ex.Message, "NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常");

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackException(new ExceptionTelemetry(ex)
                {
                    SeverityLevel = SeverityLevel.Error
                });
            }

            await WriteJsonResponse(context, HttpStatusCode.InternalServerError,
                ApiResponse.Fail("系统繁忙，请稍后重试", "SYS_500"));
        }
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        var headers = context.Request.Headers;
        return headers["X-Requested-With"] == "XMLHttpRequest" ||
               headers["Accept"].ToString().Contains("application/json");
    }

    private static async Task WriteJsonResponse(HttpContext context, HttpStatusCode statusCode, ApiResponse apiResponse)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse, options));
    }
}
