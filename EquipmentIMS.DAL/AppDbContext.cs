using Microsoft.EntityFrameworkCore;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL;

/// <summary>
/// 应用程序数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // === 系统模块 ===
    public DbSet<SysUser> SysUsers => Set<SysUser>();
    public DbSet<SysRole> SysRoles => Set<SysRole>();
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
    public DbSet<SysPermission> SysPermissions => Set<SysPermission>();
    public DbSet<SysRolePermission> SysRolePermissions => Set<SysRolePermission>();
    public DbSet<SysOperationLog> SysOperationLogs => Set<SysOperationLog>();
    public DbSet<SysPasswordHistory> SysPasswordHistories => Set<SysPasswordHistory>();
    public DbSet<SysConfig> SysConfigs => Set<SysConfig>();
    public DbSet<SysNotification> SysNotifications => Set<SysNotification>();

    // === 基础模块 ===
    public DbSet<BaseSupplier> BaseSuppliers => Set<BaseSupplier>();
    public DbSet<BaseCustomer> BaseCustomers => Set<BaseCustomer>();
    public DbSet<BaseCategory> BaseCategories => Set<BaseCategory>();
    public DbSet<BaseWarehouse> BaseWarehouses => Set<BaseWarehouse>();
    public DbSet<BaseProduct> BaseProducts => Set<BaseProduct>();
    public DbSet<BaseProductStock> BaseProductStocks => Set<BaseProductStock>();
    public DbSet<BaseCustomerPrice> BaseCustomerPrices => Set<BaseCustomerPrice>();

    // === 采购模块 ===
    public DbSet<PurPurchaseOrder> PurPurchaseOrders => Set<PurPurchaseOrder>();
    public DbSet<PurPurchaseOrderDetail> PurPurchaseOrderDetails => Set<PurPurchaseOrderDetail>();
    public DbSet<PurPurchaseIn> PurPurchaseIns => Set<PurPurchaseIn>();
    public DbSet<PurPurchaseInDetail> PurPurchaseInDetails => Set<PurPurchaseInDetail>();
    public DbSet<PurPurchaseReturn> PurPurchaseReturns => Set<PurPurchaseReturn>();
    public DbSet<PurPurchaseReturnDetail> PurPurchaseReturnDetails => Set<PurPurchaseReturnDetail>();

    // === 销售模块 ===
    public DbSet<SaleSaleOrder> SaleSaleOrders => Set<SaleSaleOrder>();
    public DbSet<SaleSaleOrderDetail> SaleSaleOrderDetails => Set<SaleSaleOrderDetail>();
    public DbSet<SaleSaleOut> SaleSaleOuts => Set<SaleSaleOut>();
    public DbSet<SaleSaleOutDetail> SaleSaleOutDetails => Set<SaleSaleOutDetail>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnDetail> SaleReturnDetails => Set<SaleReturnDetail>();

    // === 库存模块 ===
    public DbSet<InvStockTransaction> InvStockTransactions => Set<InvStockTransaction>();
    public DbSet<InvStockCheck> InvStockChecks => Set<InvStockCheck>();
    public DbSet<InvStockCheckDetail> InvStockCheckDetails => Set<InvStockCheckDetail>();
    public DbSet<InvStockTransfer> InvStockTransfers => Set<InvStockTransfer>();
    public DbSet<InvStockTransferDetail> InvStockTransferDetails => Set<InvStockTransferDetail>();
    public DbSet<InvStockAlert> InvStockAlerts => Set<InvStockAlert>();
    public DbSet<InvBackupHistory> InvBackupHistories => Set<InvBackupHistory>();

    // === 维修模块 ===
    public DbSet<MaintDeviceArchive> MaintDeviceArchives => Set<MaintDeviceArchive>();
    public DbSet<MaintRepairOrder> MaintRepairOrders => Set<MaintRepairOrder>();
    public DbSet<MaintRepairPart> MaintRepairParts => Set<MaintRepairPart>();
    public DbSet<MaintMaintenancePlan> MaintMaintenancePlans => Set<MaintMaintenancePlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
