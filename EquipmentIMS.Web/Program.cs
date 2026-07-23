using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using EquipmentIMS.BLL.Extensions;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Extensions;
using EquipmentIMS.Web;
using EquipmentIMS.Web.Filters;

var logger = LogManager.Setup().GetCurrentClassLogger();
logger.Info("应用程序启动");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Application Insights（ConnectionString 从配置文件或环境变量读取，生产环境通过环境变量覆盖）
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });

    // 数据库
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
    var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr);
    connBuilder.ConnectTimeout = 10;
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(connBuilder.ConnectionString, sqlOptions =>
            sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null));
    });

    // DAL 层服务
    builder.Services.AddDALServices();

    // BLL 层服务
    builder.Services.AddBLLServices();

    // HttpContext 访问器
    builder.Services.AddHttpContextAccessor();

    // 内存缓存
    builder.Services.AddMemoryCache();

    // Cookie 认证
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "EquipmentIMS.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.LogoutPath = "/Account/Logout";
        });

    // AntiForgery Token
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.Name = "EquipmentIMS.AF";
        options.Cookie.HttpOnly = true;
        options.HeaderName = "X-CSRF-TOKEN";
    });

    // 操作日志过滤器（全局注册）
    builder.Services.AddScoped<OperationLogActionFilter>();

    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.AddService<OperationLogActionFilter>();
    });

    // Swagger API 文档（仅开发环境启用 UI）
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "EquipmentIMS API",
            Version = "v1",
            Description = "设备库存管理系统 RESTful API 接口文档"
        });

        // 包含 XML 注释（若项目生成了 XML 文档文件）
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    });

    // Health Checks
    builder.Services.AddHealthChecks();

    // 异步导出后台服务
    builder.Services.AddHostedService<EquipmentIMS.Web.BackgroundServices.ExportBackgroundService>();

    // 报表异步导出后台服务
    builder.Services.AddHostedService<EquipmentIMS.Web.BackgroundServices.ReportExportBackgroundService>();

    // 保养提醒后台服务
    builder.Services.AddHostedService<EquipmentIMS.Web.BackgroundServices.MaintenanceReminderBackgroundService>();

    // 库存预警后台服务
    builder.Services.AddHostedService<EquipmentIMS.Web.BackgroundServices.StockAlertBackgroundService>();

    // 导出文件清理后台服务（每天清理超过 7 天的导出文件）
    builder.Services.AddHostedService<EquipmentIMS.Web.BackgroundServices.ExportFileCleanupService>();

    var app = builder.Build();

    // 开发环境：自动创建数据库并初始化种子数据
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DataSeed.SeedAsync(db);
        await DataSeed.SeedDemoAsync(db);
    }

    // 全局异常中间件（最外层）
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 安全响应头
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Swagger UI（仅开发环境）
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EquipmentIMS API V1");
            c.RoutePrefix = "swagger";
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });
    }

    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "应用启动异常");
    throw;
}
finally
{
    LogManager.Shutdown();
}
