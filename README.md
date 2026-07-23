# 设备库存管理系统 (EquipmentIMS)

基于 **ASP.NET Core MVC (.NET 8.0)** + **EF Core 8.0** + **SQL Server** 的轻量级设备进销存与维修管理系统，采用经典三层架构（Models / DAL / BLL / Web）。

---

## 系统概述

本系统面向中小型工业自动化设备代理销售与维修服务企业（如 PLC、变频器、伺服驱动器、传感器等），覆盖 **采购 → 入库 → 销售 → 出库 → 维修 → 盘点** 全生命周期，支持多角色、多仓库、多权限的 RBAC 权限体系。

### 核心功能

| 模块 | 功能 |
|---|---|
| **仪表盘** | 库存总值、本月销售额/采购额、待处理工单、低库存预警、待审批单据，卡片点击可跳转 |
| **采购管理** | 采购订单（草稿→待审批→已审批→入库→关闭）、采购入库、采购退货 |
| **销售管理** | 销售订单（审批流程）、销售出库、销售退货 |
| **库存管理** | 库存查询、库存流水、调拨（含审批）、盘点、期初导入、低库存预警 |
| **设备维修** | 设备档案（一机一档）、保养计划、维修工单（派工→开工→完工→确认→关闭） |
| **报表中心** | 库存报表、销售报表、采购报表、维修报表，支持 Excel 导出 |
| **基础数据** | 产品分类（树形）、产品管理、仓库管理、供应商管理、客户管理 |
| **系统管理** | 用户管理、角色权限（RBAC）、菜单管理、系统配置、操作日志 |
| **系统工具** | 数据库备份与下载、站内通知、操作日志审计 |

---

## 技术栈

| 类别 | 技术 | 版本 |
|---|---|---|
| 运行时 | .NET | 8.0 |
| Web 框架 | ASP.NET Core MVC | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| 数据库 | SQL Server | — |
| 认证 | ASP.NET Core Cookie Authentication | — |
| 对象映射 | Mapster | 10.* |
| 日志 | NLog | 5.* |
| Excel | NPOI | 2.7.* |
| 前端 UI | Bootstrap 5 | — |
| 图表 | ECharts | — |
| 监控 | Application Insights | 2.* |
| 测试 | xUnit + Moq + FluentAssertions + Bogus | — |
| 性能测试 | BenchmarkDotNet | — |

---

## 项目结构

```
EquipmentIMS.sln
├── EquipmentIMS.Models/      # 实体、DTO、枚举、视图模型、异常类型（共享层）
├── EquipmentIMS.DAL/         # 数据访问层：EF Core DbContext、Entity配置、仓储、工作单元
├── EquipmentIMS.BLL/         # 业务逻辑层：服务接口与实现、AutoMapper配置、DI扩展
├── EquipmentIMS.Web/         # 表现层：MVC控制器、视图（Razor）、后台服务、中间件
├── EquipmentIMS.Tests/       # 单元测试：xUnit + Moq + InMemory EF Core
├── EquipmentIMS.DataGen/     # 演示数据生成器（控制台应用，输出 SQL 脚本）
└── *.sql / *.ps1             # SQL 脚本与 PowerShell 数据生成辅助脚本
```

### 分层职责

```
Web (Controllers + Views)
  └── BLL (Business Services + DTOs)
        └── DAL (EF Core Repositories + UnitOfWork)
              └── Models (Entities + Enums + Exceptions)
```

---

## 快速开始

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server（本地或远程，支持 Windows 认证）
- Visual Studio 2022 / VS Code / Rider（可选）

### 1. 配置数据库连接

编辑 `EquipmentIMS.Web/appsettings.json`：

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=EquipmentIMS;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. 运行项目

```bash
cd EquipmentIMS.Web
dotnet run
```

开发环境下首次启动会自动创建数据库并初始化种子数据（管理员账号 + 演示数据）。

访问地址：`http://localhost:5000`（端口以控制台输出为准）

### 3. 生成演示数据（可选）

```bash
cd EquipmentIMS.DataGen
dotnet run -- -y
```

执行后将生成 `demo_data.sql` 脚本，使用 SQL Server Management Studio 或 `sqlcmd` 执行导入。

---

## 默认账号

| 角色 | 用户名 | 密码 |
|---|---|---|
| 超级管理员 | admin | admin123 |
| 仓库管理员 | warehouse | admin123 |
| 销售员 | sale | admin123 |
| 采购员 | purchase | admin123 |
| 技术员 | tech | admin123 |

---

## 构建与发布

```bash
# Debug 构建
dotnet build

# Release 构建
dotnet build -c Release

# 发布（自包含）
dotnet publish EquipmentIMS.Web -c Release -o ./publish
```

---

## 运行测试

```bash
dotnet test
```

测试框架：xUnit + Moq（Mock 依赖）+ FluentAssertions（断言）+ InMemory EF Core（数据库模拟）+ Bogus（测试数据生成）。

---

## API 文档

开发环境下访问 `/swagger` 可查看基于 Swagger 的 RESTful API 交互文档。

---

## 系统特性

- **RBAC 权限体系**：用户 → 角色 → 权限（菜单权限 + 按钮权限），细粒度控制
- **乐观锁并发控制**：基于 RowVersion 防止库存超卖与数据冲突
- **库存流水追踪**：所有库存变动记录流水，包含业务单号、操作类型、变更数量、操作人
- **操作日志审计**：全局 ActionFilter 拦截关键操作，记录操作人、IP、参数、结果
- **站内通知**：低库存预警、保养到期、审批提醒
- **后台定时任务**：库存预警检查、保养到期提醒、导出文件清理
- **数据库备份**：支持全量备份、RESTORE VERIFYONLY 完整性校验、备份文件下载
- **Excel 导入导出**：产品批量导入、报表导出、库存流水导出
- **全局异常处理**：统一异常中间件，友好错误页面
- **安全响应头**：SecurityHeadersMiddleware 增强 Web 安全

---

## 许可证

内部项目，仅供授权人员使用。
