using EquipmentIMS.DAL;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Web;

public static class DataSeed
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.SysUsers.Any()) return;

        var now = DateTime.Now;
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<SysUser>();

        // === 管理员用户 ===
        var admin = new SysUser
        {
            UserName = "admin", RealName = "系统管理员",
            PasswordHash = hasher.HashPassword(null!, "admin123"),
            PasswordSalt = string.Empty, IsActive = true, MustChangePwd = false, CreateTime = now
        };
        db.SysUsers.Add(admin);
        await db.SaveChangesAsync();

        // === 角色 ===
        var adminRole = new SysRole { RoleCode = "admin", RoleName = "系统管理员", Description = "拥有所有权限", CreateTime = now };
        var mgrRole = new SysRole { RoleCode = "manager", RoleName = "部门经理", Description = "部门经理权限", CreateTime = now };
        db.SysRoles.Add(adminRole);
        db.SysRoles.Add(mgrRole);
        await db.SaveChangesAsync();

        db.SysUserRoles.Add(new SysUserRole { UserId = admin.Id, RoleId = adminRole.Id });

        // === 菜单权限树 ===
        var menuCodeMap = new Dictionary<string, long>();
        foreach (var m in MenuDefs)
        {
            long? pid = m.ParentCode == null ? null : menuCodeMap[m.ParentCode];
            var p = new SysPermission
            {
                ParentId = pid, PermissionCode = m.Code, PermissionName = m.Name,
                PermissionType = PermissionType.Menu, Url = m.Url, Icon = m.Icon,
                SortOrder = m.Sort, CreateTime = now
            };
            db.SysPermissions.Add(p);
            await db.SaveChangesAsync();
            menuCodeMap[m.Code] = p.Id;
        }

        // === 按钮权限 ===
        foreach (var (code, name, parentCode, sort) in BtnDefs)
        {
            db.SysPermissions.Add(new SysPermission
            {
                ParentId = menuCodeMap[parentCode], PermissionCode = code, PermissionName = name,
                PermissionType = PermissionType.Button, SortOrder = sort, CreateTime = now
            });
        }
        await db.SaveChangesAsync();

        // === admin 拥有全部权限 ===
        foreach (var p in db.SysPermissions.ToList())
            db.SysRolePermissions.Add(new SysRolePermission { RoleId = adminRole.Id, PermissionId = p.Id });
        await db.SaveChangesAsync();

        // === 系统配置 ===
        var configs = new (string Key, string Name, string Value, string Desc, ConfigType Type)[]
        {
            ("CompanyName","公司名称","设备库存管理系统","公司名称",ConfigType.String),
            ("CompanyAddress","公司地址","","公司地址",ConfigType.String),
            ("CompanyPhone","联系方式","","公司联系方式",ConfigType.String),
            ("DocumentPrefix_Purchase","采购单据前缀","CG","采购单据编号前缀",ConfigType.String),
            ("DocumentPrefix_Sale","销售单据前缀","XS","销售单据编号前缀",ConfigType.String),
            ("DocumentPrefix_Transfer","调拨单据前缀","DB","调拨单据编号前缀",ConfigType.String),
            ("StockAlertCheckCron","低库存预警检查时间","08:00","每日预警检查时间（格式 HH:mm）",ConfigType.String),
            ("PasswordExpiryDays","密码过期天数","90","密码过期天数（0=不限制）",ConfigType.Number),
            ("MaxLoginAttempts","最大登录失败次数","5","登录失败锁定阈值",ConfigType.Number),
            ("LockoutMinutes","锁定时间(分钟)","30","登录失败锁定分钟数",ConfigType.Number),
            ("PasswordHistoryCount","密码历史数量","5","禁止重复使用的历史密码数",ConfigType.Number),
            ("CreditCheckEnabled","信用额度检查开关","false","是否启用客户信用额度检查",ConfigType.Boolean),
        };
        foreach (var c in configs)
            db.SysConfigs.Add(new SysConfig { ConfigKey = c.Key, ConfigName = c.Name, ConfigValue = c.Value, Description = c.Desc, ConfigType = c.Type, IsEditable = true, CreateTime = now });
        await db.SaveChangesAsync();
    }

    public static async Task SeedDemoAsync(AppDbContext db)
    {
        if (db.BaseProducts.Any()) return;
        var now = DateTime.Now;

        // === 分类 ===
        var cats = new[] {
            new BaseCategory { Code="NETWORK", Name="网络设备", Level=1, SortOrder=1, CreateTime=now },
            new BaseCategory { Code="SERVER", Name="服务器", Level=1, SortOrder=2, CreateTime=now },
            new BaseCategory { Code="STORAGE", Name="存储设备", Level=1, SortOrder=3, CreateTime=now },
            new BaseCategory { Code="SECURITY", Name="安全设备", Level=1, SortOrder=4, CreateTime=now },
        };
        db.BaseCategories.AddRange(cats);
        await db.SaveChangesAsync();

        // === 供应商 ===
        var sups = new[] {
            new BaseSupplier { Code="SUP001", Name="华为技术有限公司", ContactPerson="张伟", ContactPhone="13800138001", Address="深圳市龙岗区", CreateTime=now },
            new BaseSupplier { Code="SUP002", Name="思科系统(中国)", ContactPerson="李明", ContactPhone="13800138002", Address="北京市朝阳区", CreateTime=now },
            new BaseSupplier { Code="SUP003", Name="浪潮电子信息", ContactPerson="王强", ContactPhone="13800138003", Address="济南市高新区", CreateTime=now },
        };
        db.BaseSuppliers.AddRange(sups);
        await db.SaveChangesAsync();

        // === 客户 ===
        var custs = new[] {
            new BaseCustomer { Code="CUS001", Name="腾讯科技有限公司", ContactPerson="赵刚", ContactPhone="13900139001", Address="深圳市南山区", CreditLimit=5000000, CreateTime=now },
            new BaseCustomer { Code="CUS002", Name="阿里巴巴集团", ContactPerson="孙丽", ContactPhone="13900139002", Address="杭州市余杭区", CreditLimit=3000000, CreateTime=now },
            new BaseCustomer { Code="CUS003", Name="中国移动通信", ContactPerson="周杰", ContactPhone="13900139003", Address="北京市西城区", CreditLimit=8000000, CreateTime=now },
        };
        db.BaseCustomers.AddRange(custs);
        await db.SaveChangesAsync();

        // === 仓库 ===
        var whs = new[] {
            new BaseWarehouse { Code="WH001", Name="主仓库", Address="深圳市南山区科技园", Manager="刘磊", Phone="0755-88880001", IsDefault=true, CreateTime=now },
            new BaseWarehouse { Code="WH002", Name="备件仓库", Address="深圳市龙华区", Manager="陈芳", Phone="0755-88880002", IsDefault=false, CreateTime=now },
        };
        db.BaseWarehouses.AddRange(whs);
        await db.SaveChangesAsync();

        // === 设备 ===
        var prods = new[] {
            new BaseProduct { Code="PROD001", Name="华为 AR6140 路由器", CategoryId=cats[0].Id, Spec="AR6140-16G4XG", Brand="华为", Unit="台", PurchasePrice=8500m, SalePrice=12000m, MinStock=5, CreateTime=now },
            new BaseProduct { Code="PROD002", Name="华为 S5735-L48P4X 交换机", CategoryId=cats[0].Id, Spec="48口PoE+4万兆上行", Brand="华为", Unit="台", PurchasePrice=6800m, SalePrice=9500m, MinStock=3, CreateTime=now },
            new BaseProduct { Code="PROD003", Name="H3C SR6608 路由器", CategoryId=cats[0].Id, Spec="SR6608", Brand="H3C", Unit="台", PurchasePrice=28000m, SalePrice=38000m, MinStock=2, CreateTime=now },
            new BaseProduct { Code="PROD004", Name="戴尔 PowerEdge R750 服务器", CategoryId=cats[1].Id, Spec="2*Xeon 4314/128G/4*2.4T", Brand="戴尔", Unit="台", PurchasePrice=65000m, SalePrice=88000m, MinStock=2, CreateTime=now },
            new BaseProduct { Code="PROD005", Name="浪潮 NF5280M6 服务器", CategoryId=cats[1].Id, Spec="2*Xeon 4310/64G/3*1.2T", Brand="浪潮", Unit="台", PurchasePrice=42000m, SalePrice=58000m, MinStock=2, CreateTime=now },
            new BaseProduct { Code="PROD006", Name="华为 OceanStor 5210 存储", CategoryId=cats[2].Id, Spec="2U/双控/25*2.4T SAS", Brand="华为", Unit="台", PurchasePrice=95000m, SalePrice=135000m, MinStock=1, CreateTime=now },
            new BaseProduct { Code="PROD007", Name="深信服 AF-2000 防火墙", CategoryId=cats[3].Id, Spec="AF-2000-FH", Brand="深信服", Unit="台", PurchasePrice=35000m, SalePrice=52000m, MinStock=2, CreateTime=now },
            new BaseProduct { Code="PROD008", Name="奇安信 NSG5000 防火墙", CategoryId=cats[3].Id, Spec="NSG5000-TG25M", Brand="奇安信", Unit="台", PurchasePrice=48000m, SalePrice=68000m, MinStock=1, CreateTime=now },
        };
        db.BaseProducts.AddRange(prods);
        await db.SaveChangesAsync();

        // === 库存 ===
        var stocks = new[] {
            new BaseProductStock { ProductId=prods[0].Id, WarehouseId=whs[0].Id, Quantity=25, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[1].Id, WarehouseId=whs[0].Id, Quantity=18, LockQty=2, CreateTime=now },
            new BaseProductStock { ProductId=prods[2].Id, WarehouseId=whs[0].Id, Quantity=8, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[3].Id, WarehouseId=whs[0].Id, Quantity=12, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[4].Id, WarehouseId=whs[0].Id, Quantity=6, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[5].Id, WarehouseId=whs[0].Id, Quantity=5, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[6].Id, WarehouseId=whs[0].Id, Quantity=10, LockQty=1, CreateTime=now },
            new BaseProductStock { ProductId=prods[7].Id, WarehouseId=whs[0].Id, Quantity=3, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[0].Id, WarehouseId=whs[1].Id, Quantity=8, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[1].Id, WarehouseId=whs[1].Id, Quantity=5, LockQty=0, CreateTime=now },
            new BaseProductStock { ProductId=prods[4].Id, WarehouseId=whs[1].Id, Quantity=3, LockQty=0, CreateTime=now },
        };
        db.BaseProductStocks.AddRange(stocks);
        await db.SaveChangesAsync();

        // === 采购订单 ===
        var pur1 = new PurPurchaseOrder
        {
            OrderNo = $"CG{now:yyyyMMdd}001", SupplierId = sups[0].Id, WarehouseId = whs[0].Id,
            OrderDate = now.AddDays(-15), TotalAmount = 170000m,
            Status = PurchaseOrderStatus.Approved, Approver = "admin", ApproveTime = now.AddDays(-14),
            CreateBy = "admin", CreateTime = now.AddDays(-15),
            Details = new List<PurPurchaseOrderDetail>
            {
                new() { ProductId=prods[0].Id, Qty=10, UnitPrice=8500m, Amount=85000m, CreateTime=now.AddDays(-15) },
                new() { ProductId=prods[1].Id, Qty=10, UnitPrice=6800m, Amount=68000m, CreateTime=now.AddDays(-15) },
                new() { ProductId=prods[2].Id, Qty=1, UnitPrice=17000m, Amount=17000m, CreateTime=now.AddDays(-15) },
            }
        };
        var pur2 = new PurPurchaseOrder
        {
            OrderNo = $"CG{now:yyyyMMdd}002", SupplierId = sups[1].Id, WarehouseId = whs[0].Id,
            OrderDate = now.AddDays(-3), TotalAmount = 48000m,
            Status = PurchaseOrderStatus.PendingApproval,
            CreateBy = "admin", CreateTime = now.AddDays(-3),
            Details = new List<PurPurchaseOrderDetail>
            {
                new() { ProductId=prods[5].Id, Qty=2, UnitPrice=24000m, Amount=48000m, CreateTime=now.AddDays(-3) },
            }
        };
        db.PurPurchaseOrders.Add(pur1);
        db.PurPurchaseOrders.Add(pur2);
        await db.SaveChangesAsync();

        // === 销售订单 ===
        var sale1 = new SaleSaleOrder
        {
            OrderNo = $"XS{now:yyyyMMdd}001", CustomerId = custs[0].Id,
            OrderDate = now.AddDays(-10), TotalAmount = 360000m,
            Status = SaleOrderStatus.Approved, Approver = "admin", ApproveTime = now.AddDays(-9),
            CreateBy = "admin", CreateTime = now.AddDays(-10),
            Details = new List<SaleSaleOrderDetail>
            {
                new() { ProductId=prods[3].Id, WarehouseId=whs[0].Id, Qty=2, UnitPrice=88000m, Amount=176000m, CreateTime=now.AddDays(-10) },
                new() { ProductId=prods[6].Id, WarehouseId=whs[0].Id, Qty=2, UnitPrice=52000m, Amount=104000m, CreateTime=now.AddDays(-10) },
                new() { ProductId=prods[0].Id, WarehouseId=whs[0].Id, Qty=5, UnitPrice=12000m, Amount=60000m, CreateTime=now.AddDays(-10) },
                new() { ProductId=prods[7].Id, WarehouseId=whs[0].Id, Qty=1, UnitPrice=20000m, Amount=20000m, CreateTime=now.AddDays(-10) },
            }
        };
        var sale2 = new SaleSaleOrder
        {
            OrderNo = $"XS{now:yyyyMMdd}002", CustomerId = custs[1].Id,
            OrderDate = now.AddDays(-2), TotalAmount = 190000m,
            Status = SaleOrderStatus.PendingApproval,
            CreateBy = "admin", CreateTime = now.AddDays(-2),
            Details = new List<SaleSaleOrderDetail>
            {
                new() { ProductId=prods[3].Id, WarehouseId=whs[0].Id, Qty=1, UnitPrice=88000m, Amount=88000m, CreateTime=now.AddDays(-2) },
                new() { ProductId=prods[5].Id, WarehouseId=whs[0].Id, Qty=1, UnitPrice=54000m, Amount=54000m, CreateTime=now.AddDays(-2) },
                new() { ProductId=prods[4].Id, WarehouseId=whs[0].Id, Qty=1, UnitPrice=48000m, Amount=48000m, CreateTime=now.AddDays(-2) },
            }
        };
        db.SaleSaleOrders.Add(sale1);
        db.SaleSaleOrders.Add(sale2);
        await db.SaveChangesAsync();
    }

    // 菜单定义: (Code, Name, ParentCode, Url, Icon, Sort)
    private static readonly (string Code, string Name, string? ParentCode, string Url, string Icon, int Sort)[] MenuDefs =
    {
        ("home:dashboard", "首页", null, "/Home/Index", "bi-house", 0),
        ("base:menu", "基础数据", null, "#", "bi-database", 1),
        ("base:supplier", "供应商管理", "base:menu", "/Supplier/Index", "bi-building", 11),
        ("base:customer", "客户管理", "base:menu", "/Customer/Index", "bi-people", 12),
        ("base:warehouse", "仓库管理", "base:menu", "/Warehouse/Index", "bi-boxes", 13),
        ("base:category", "设备分类", "base:menu", "/Category/Index", "bi-tags", 14),
        ("base:product", "设备管理", "base:menu", "/Product/Index", "bi-cpu", 15),
        ("pur:menu", "采购管理", null, "#", "bi-cart-plus", 2),
        ("pur:order", "采购订单", "pur:menu", "/PurchaseOrder/Index", "bi-cart-check", 21),
        ("pur:in", "采购入库", "pur:menu", "/PurchaseIn/Index", "bi-box-arrow-in-down", 22),
        ("pur:return", "采购退货", "pur:menu", "/PurchaseReturn/Index", "bi-arrow-return-left", 23),
        ("sale:menu", "销售管理", null, "#", "bi-cart-dash", 3),
        ("sale:order", "销售订单", "sale:menu", "/SaleOrder/Index", "bi-cart", 31),
        ("sale:out", "销售出库", "sale:menu", "/SaleOut/Index", "bi-box-arrow-up", 32),
        ("sale:return", "销售退货", "sale:menu", "/SaleReturn/Index", "bi-arrow-return-right", 33),
        ("stock:menu", "库存管理", null, "#", "bi-box-seam", 4),
        ("stock:view", "库存查询", "stock:menu", "/Stock/Index", "bi-search", 41),
        ("stock:transfer", "库存调拨", "stock:menu", "/StockTransfer/Index", "bi-arrow-left-right", 42),
        ("stock:check","库存盘点","stock:menu","/StockCheck/Index","bi-clipboard-check",43),
        ("maint:menu","设备维保",null,"#","bi-tools",7),
        ("maint:archive","设备档案","maint:menu","/DeviceArchive/Index","bi-archive",71),
        ("maint:plan","保养计划","maint:menu","/MaintenancePlan/Index","bi-calendar-check",72),
        ("maint:repair","维修工单","maint:menu","/RepairOrder/Index","bi-wrench",73),
        ("report:menu","报表中心",null,"#","bi-bar-chart",5),
        ("report:stock", "库存报表", "report:menu", "/Report/Inventory", "bi-file-earmark-bar-graph", 51),
        ("report:sales", "销售报表", "report:menu", "/Report/Sale", "bi-graph-up", 52),
        ("sys:menu", "系统管理", null, "#", "bi-gear", 6),
        ("sys:user", "用户管理", "sys:menu", "/User/Index", "bi-person-badge", 61),
        ("sys:role", "角色管理", "sys:menu", "/Role/Index", "bi-shield-lock", 62),
        ("sys:log", "操作日志", "sys:menu", "/Log/Index", "bi-journal-text", 63),
        ("sys:config", "系统配置", "sys:menu", "/SystemConfig/Index", "bi-sliders", 64),
    };

    // 按钮定义: (Code, Name, ParentMenuCode, Sort)
    private static readonly (string Code, string Name, string ParentMenuCode, int Sort)[] BtnDefs =
    {
        ("base:supplier:view","查看","base:supplier",0),("base:supplier:create","新增","base:supplier",1),("base:supplier:edit","编辑","base:supplier",2),("base:supplier:delete","删除","base:supplier",3),
        ("base:customer:view","查看","base:customer",0),("base:customer:create","新增","base:customer",1),("base:customer:edit","编辑","base:customer",2),("base:customer:delete","删除","base:customer",3),("base:customer:price","客户价格","base:customer",4),
        ("base:category:view","查看","base:category",0),("base:category:create","新增","base:category",1),("base:category:edit","编辑","base:category",2),("base:category:delete","删除","base:category",3),
        ("base:warehouse:view","查看","base:warehouse",0),("base:warehouse:create","新增","base:warehouse",1),("base:warehouse:edit","编辑","base:warehouse",2),("base:warehouse:delete","删除","base:warehouse",3),
        ("base:product:view","查看","base:product",0),("base:product:create","新增","base:product",1),("base:product:edit","编辑","base:product",2),("base:product:delete","删除","base:product",3),("base:product:import","导入","base:product",4),
        ("pur:order:view","查看","pur:order",0),("pur:order:create","新增","pur:order",1),("pur:order:detail","明细","pur:order",2),("pur:order:submit","提交","pur:order",3),("pur:order:approve","审批","pur:order",4),("pur:order:cancel","取消","pur:order",5),
        ("pur:in:view","查看","pur:in",0),("pur:in:create","新增","pur:in",1),
        ("pur:return:view","查看","pur:return",0),("pur:return:create","新增","pur:return",1),
        ("sale:order:view","查看","sale:order",0),("sale:order:create","新增","sale:order",1),("sale:order:detail","明细","sale:order",2),("sale:order:submit","提交","sale:order",3),("sale:order:approve","审批","sale:order",4),("sale:order:cancel","取消","sale:order",5),
        ("sale:out:view","查看","sale:out",0),("sale:out:create","新增","sale:out",1),("sale:out:detail","明细","sale:out",2),
        ("sale:return:view","查看","sale:return",0),("sale:return:create","新增","sale:return",1),
        ("stock:transfer:view","查看","stock:transfer",0),("stock:transfer:create","新增","stock:transfer",1),("stock:transfer:detail","明细","stock:transfer",2),("stock:transfer:submit","提交","stock:transfer",3),("stock:transfer:approve","审批","stock:transfer",4),("stock:transfer:reject","驳回","stock:transfer",5),("stock:transfer:execute","执行","stock:transfer",6),("stock:transfer:cancel","取消","stock:transfer",7),
        ("stock:check:view","查看","stock:check",0),("stock:check:create","新增","stock:check",1),("stock:check:detail","明细","stock:check",2),("stock:check:record","盘入记录","stock:check",3),("stock:check:submit","提交","stock:check",4),("stock:check:approve","审批","stock:check",5),("stock:check:cancel","取消","stock:check",6),
        ("stock:import","导入","stock:view",1),
        ("report:view","查看","report:menu",0),
        ("maint:archive:view","查看","maint:archive",0),("maint:archive:create","新增","maint:archive",1),("maint:archive:edit","编辑","maint:archive",2),("maint:archive:delete","删除","maint:archive",3),
        ("maint:plan:view","查看","maint:plan",0),("maint:plan:create","新增","maint:plan",1),("maint:plan:edit","编辑","maint:plan",2),("maint:plan:delete","删除","maint:plan",3),
        ("maint:repair:view","查看","maint:repair",0),("maint:repair:create","新增","maint:repair",1),("maint:repair:dispatch","派工","maint:repair",2),("maint:repair:process","维修","maint:repair",3),("maint:repair:confirm","确认","maint:repair",4),("maint:repair:close","关闭","maint:repair",5),("maint:repair:cancel","取消","maint:repair",6),
        ("sys:user:view","查看","sys:user",0),("sys:user:create","新增","sys:user",1),("sys:user:edit","编辑","sys:user",2),("sys:user:delete","删除","sys:user",3),("sys:user:role","分配角色","sys:user",4),
        ("sys:role:view","查看","sys:role",0),("sys:role:create","新增","sys:role",1),("sys:role:edit","编辑","sys:role",2),("sys:role:delete","删除","sys:role",3),("sys:role:perm","分配权限","sys:role",4),
        ("sys:log:view","查看","sys:log",0),
        ("sys:config:view","查看","sys:config",0),("sys:config:edit","编辑","sys:config",1),
        ("sys:backup:view","查看","sys:config",2),("sys:backup:create","创建备份","sys:config",3),
    };
}
