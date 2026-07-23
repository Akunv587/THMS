-- ============================================================
-- 设备库存管理系统 - 系统模块演示数据 (批次0)
-- 基准日期: 2026-07-17
-- 公司: 郑州恒锐自动化设备有限公司
-- ============================================================
SET NOCOUNT ON;

BEGIN TRY
BEGIN TRAN;

-- ===== 1. Sys_Role (5行) =====
SET IDENTITY_INSERT Sys_Role ON;
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES(1,'admin',N'系统管理员',N'拥有系统全部权限，可管理用户/角色/配置',0,'2026-07-17 08:00:00',N'likun');
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES(2,'manager',N'经理',N'可查看所有数据、报表，审批采购/销售单据',0,'2026-07-17 08:00:00',N'likun');
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES(3,'warehouse',N'仓库管理员',N'管理库存、采购入库/退货、盘点、调拨',0,'2026-07-17 08:00:00',N'likun');
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES(4,'sales',N'销售员',N'管理客户、销售订单/出库/退货、客户价格',0,'2026-07-17 08:00:00',N'likun');
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES(5,'technician',N'维修技术员',N'管理设备档案、维修工单、保养计划',0,'2026-07-17 08:00:00',N'likun');
SET IDENTITY_INSERT Sys_Role OFF;
GO
