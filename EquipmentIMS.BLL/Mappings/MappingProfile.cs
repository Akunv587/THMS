using Mapster;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Mappings;

/// <summary>
/// Mapster 实体→DTO 映射配置
/// </summary>
public static class MappingProfile
{
    public static TypeAdapterConfig Configure()
    {
        var config = new TypeAdapterConfig();

        // === 系统模块 ===
        config.NewConfig<SysUser, UserDto>()
            .Ignore(dest => dest.Roles);

        config.NewConfig<SysRole, RoleDto>()
            .Ignore(dest => dest.PermissionIds);

        config.NewConfig<SysConfig, SystemConfigDto>()
            .Map(dest => dest.ConfigType, src => (int)src.ConfigType);

        config.NewConfig<SysNotification, NotificationDto>()
            .Map(dest => dest.NotificationType, src => (int)src.NotificationType);

        // === 基础模块 ===
        config.NewConfig<BaseProductStock, StockQueryDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null)
            .Map(dest => dest.Spec, src => src.Product != null ? src.Product.Spec : null)
            .Map(dest => dest.Brand, src => src.Product != null ? src.Product.Brand : null)
            .Map(dest => dest.CategoryName, src => src.Product != null && src.Product.Category != null ? src.Product.Category.Name : null)
            .Map(dest => dest.WarehouseName, src => src.Warehouse != null ? src.Warehouse.Name : null)
            .Map(dest => dest.MinStock, src => src.Product != null ? src.Product.MinStock : 0)
            .Map(dest => dest.AvailableQty, src => src.Quantity - src.LockQty > 0 ? src.Quantity - src.LockQty : 0)
            .Map(dest => dest.WarningLevel, src => src.Quantity <= (src.Product != null ? src.Product.MinStock : 0) ? "预警" : "正常");

        // === 采购模块 ===
        config.NewConfig<PurPurchaseOrder, PurchaseOrderDto>()
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : null)
            .Map(dest => dest.WarehouseName, src => src.Warehouse != null ? src.Warehouse.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<PurPurchaseOrderDetail, PurchaseOrderDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null)
            .Map(dest => dest.Spec, src => src.Product != null ? src.Product.Spec : null);

        config.NewConfig<PurPurchaseIn, PurchaseInDto>()
            .Map(dest => dest.PurchaseOrderNo, src => src.PurchaseOrder != null ? src.PurchaseOrder.OrderNo : null)
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : null)
            .Map(dest => dest.WarehouseName, src => src.Warehouse != null ? src.Warehouse.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<PurPurchaseInDetail, PurchaseInDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null);

        // === 销售模块 ===
        config.NewConfig<SaleSaleOrder, SaleOrderDto>()
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<SaleSaleOrderDetail, SaleOrderDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null)
            .Map(dest => dest.Spec, src => src.Product != null ? src.Product.Spec : null);

        config.NewConfig<SaleSaleOut, SaleOutDto>()
            .Map(dest => dest.SaleOrderNo, src => src.SaleOrder != null ? src.SaleOrder.OrderNo : null)
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null)
            .Map(dest => dest.WarehouseName, src => src.Warehouse != null ? src.Warehouse.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<SaleSaleOutDetail, SaleOutDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null);

        // === 库存模块 ===
        config.NewConfig<InvStockCheck, StockCheckDto>()
            .Map(dest => dest.WarehouseName, src => src.Warehouse != null ? src.Warehouse.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<InvStockCheckDetail, StockCheckDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null);

        config.NewConfig<InvStockTransfer, StockTransferDto>()
            .Map(dest => dest.FromWarehouseName, src => src.FromWarehouse != null ? src.FromWarehouse.Name : null)
            .Map(dest => dest.ToWarehouseName, src => src.ToWarehouse != null ? src.ToWarehouse.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Details);

        config.NewConfig<InvStockTransferDetail, StockTransferDetailDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null);

        // === 维修模块 ===
        config.NewConfig<MaintDeviceArchive, DeviceArchiveDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null)
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status);

        config.NewConfig<MaintRepairOrder, RepairOrderDto>()
            .Map(dest => dest.SerialNo, src => src.DeviceArchive != null ? src.DeviceArchive.SerialNo : null)
            .Map(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null)
            .Map(dest => dest.Status, src => (int)src.Status)
            .Ignore(dest => dest.Parts);

        config.NewConfig<MaintRepairPart, RepairPartDto>()
            .Map(dest => dest.ProductCode, src => src.Product != null ? src.Product.Code : null)
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : null);

        config.NewConfig<MaintMaintenancePlan, MaintenancePlanDto>()
            .Map(dest => dest.SerialNo, src => src.DeviceArchive != null ? src.DeviceArchive.SerialNo : null)
            .Map(dest => dest.CycleType, src => (int)src.CycleType);

        // 全局：枚举→int，decimal保持精度
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);

        return config;
    }
}
