-- ============================================================
-- 郑州恒锐自动化设备有限公司 设备库存管理系统 演示数据
-- 生成日期: 2026-07-17 | SQL Server 2019/2022
-- ============================================================
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

-- ===== 1. 系统模块 =====
SET IDENTITY_INSERT Sys_Role ON;
INSERT Sys_Role(Id,RoleCode,RoleName,Description,IsDeleted,CreateTime,CreateBy) VALUES
(1,N'admin',N'系统管理员',N'全部权限',0,'2025-07-01 08:00',N'system'),
(2,N'manager',N'经理',N'审批/报表/配置',0,'2025-07-01 08:00',N'system'),
(3,N'warehouse',N'仓库管理员',N'出入库/盘点/调拨',0,'2025-07-01 08:00',N'system'),
(4,N'sales',N'销售员',N'销售/客户',0,'2025-07-01 08:00',N'system'),
(5,N'technician',N'维修技术员',N'维修/保养',0,'2025-07-01 08:00',N'system');
SET IDENTITY_INSERT Sys_Role OFF;

SET IDENTITY_INSERT Sys_User ON;
INSERT Sys_User(Id,UserName,RealName,Email,Phone,PasswordHash,PasswordSalt,MustChangePwd,IsActive,LastLoginTime,LastPasswordChanged,IsDeleted,CreateTime,CreateBy) VALUES
(1,N'admin',N'管理员',N'admin@hengrauto.com',N'13803710001',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-17 07:30','2025-07-01',0,'2025-07-01 08:00',N'system'),
(2,N'zhangwei',N'张伟',N'zhangwei@hengrauto.com',N'13903710002',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-16 17:45','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(3,N'likun',N'李坤',N'likun@hengrauto.com',N'15003710003',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-17 08:15','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(4,N'wangfang',N'王芳',N'wangfang@hengrauto.com',N'18603710004',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-16 18:00','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(5,N'liujie',N'刘杰',N'liujie@hengrauto.com',N'13703710005',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-17 09:00','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(6,N'zhaomin',N'赵敏',N'zhaomin@hengrauto.com',N'15803710006',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-17 08:30','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(7,N'chenhui',N'陈辉',N'chenhui@hengrauto.com',N'13503710007',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-16 16:20','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(8,N'maxiang',N'马翔',N'maxiang@hengrauto.com',N'13303710008',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-17 07:50','2025-07-01',0,'2025-07-01 08:00',N'admin'),
(9,N'guoqiang',N'郭强',N'guoqiang@hengrauto.com',N'18803710009',N'HASH_PLACE',N'SALT_PLACE',1,1,'2026-07-15 14:00','2025-07-01',0,'2025-07-01 08:00',N'admin');
SET IDENTITY_INSERT Sys_User OFF;

SET IDENTITY_INSERT Sys_UserRole ON;
INSERT Sys_UserRole(Id,UserId,RoleId) VALUES (1,1,1),(2,2,2),(3,3,3),(4,4,3),(5,5,4),(6,6,4),(7,7,4),(8,8,5),(9,9,5);
SET IDENTITY_INSERT Sys_UserRole OFF;

SET IDENTITY_INSERT Sys_Permission ON;
INSERT Sys_Permission(Id,ParentId,PermissionCode,PermissionName,PermissionType,Url,Icon,SortOrder,IsDeleted,CreateTime,CreateBy) VALUES
(1,NULL,N'dashboard:menu',N'仪表盘',1,N'/Dashboard',N'fa-dashboard',0,0,'2025-07-01 08:00',N'system'),
(2,1,N'dashboard:view',N'查看',2,NULL,NULL,1,0,'2025-07-01 08:00',N'system'),
(3,1,N'dashboard:create',N'新增',2,NULL,NULL,2,0,'2025-07-01 08:00',N'system'),
(4,1,N'dashboard:edit',N'编辑',2,NULL,NULL,3,0,'2025-07-01 08:00',N'system'),
(5,1,N'dashboard:delete',N'删除',2,NULL,NULL,4,0,'2025-07-01 08:00',N'system'),
(6,1,N'dashboard:approve',N'审批',2,NULL,NULL,5,0,'2025-07-01 08:00',N'system'),
(7,1,N'dashboard:export',N'导出',2,NULL,NULL,6,0,'2025-07-01 08:00',N'system'),
(8,NULL,N'purchase:menu',N'采购管理',1,N'/Stock/Purchase',N'fa-shopping-cart',10,0,'2025-07-01 08:00',N'system'),
(9,8,N'purchase:view',N'查看',2,NULL,NULL,11,0,'2025-07-01 08:00',N'system'),
(10,8,N'purchase:create',N'新增',2,NULL,NULL,12,0,'2025-07-01 08:00',N'system'),
(11,8,N'purchase:edit',N'编辑',2,NULL,NULL,13,0,'2025-07-01 08:00',N'system'),
(12,8,N'purchase:delete',N'删除',2,NULL,NULL,14,0,'2025-07-01 08:00',N'system'),
(13,8,N'purchase:approve',N'审批',2,NULL,NULL,15,0,'2025-07-01 08:00',N'system'),
(14,8,N'purchase:export',N'导出',2,NULL,NULL,16,0,'2025-07-01 08:00',N'system'),
(15,NULL,N'sale:menu',N'销售管理',1,N'/Stock/Sale',N'fa-shopping-bag',20,0,'2025-07-01 08:00',N'system'),
(16,15,N'sale:view',N'查看',2,NULL,NULL,21,0,'2025-07-01 08:00',N'system'),
(17,15,N'sale:create',N'新增',2,NULL,NULL,22,0,'2025-07-01 08:00',N'system'),
(18,15,N'sale:edit',N'编辑',2,NULL,NULL,23,0,'2025-07-01 08:00',N'system'),
(19,15,N'sale:delete',N'删除',2,NULL,NULL,24,0,'2025-07-01 08:00',N'system'),
(20,15,N'sale:approve',N'审批',2,NULL,NULL,25,0,'2025-07-01 08:00',N'system'),
(21,15,N'sale:export',N'导出',2,NULL,NULL,26,0,'2025-07-01 08:00',N'system'),
(22,NULL,N'stock:menu',N'库存管理',1,N'/Stock',N'fa-warehouse',30,0,'2025-07-01 08:00',N'system'),
(23,22,N'stock:view',N'查看',2,NULL,NULL,31,0,'2025-07-01 08:00',N'system'),
(24,22,N'stock:create',N'新增',2,NULL,NULL,32,0,'2025-07-01 08:00',N'system'),
(25,22,N'stock:edit',N'编辑',2,NULL,NULL,33,0,'2025-07-01 08:00',N'system'),
(26,22,N'stock:delete',N'删除',2,NULL,NULL,34,0,'2025-07-01 08:00',N'system'),
(27,22,N'stock:approve',N'审批',2,NULL,NULL,35,0,'2025-07-01 08:00',N'system'),
(28,22,N'stock:export',N'导出',2,NULL,NULL,36,0,'2025-07-01 08:00',N'system'),
(29,NULL,N'maint:menu',N'维修管理',1,N'/Maint',N'fa-wrench',40,0,'2025-07-01 08:00',N'system'),
(30,29,N'maint:view',N'查看',2,NULL,NULL,41,0,'2025-07-01 08:00',N'system'),
(31,29,N'maint:create',N'新增',2,NULL,NULL,42,0,'2025-07-01 08:00',N'system'),
(32,29,N'maint:edit',N'编辑',2,NULL,NULL,43,0,'2025-07-01 08:00',N'system'),
(33,29,N'maint:delete',N'删除',2,NULL,NULL,44,0,'2025-07-01 08:00',N'system'),
(34,29,N'maint:approve',N'审批',2,NULL,NULL,45,0,'2025-07-01 08:00',N'system'),
(35,29,N'maint:export',N'导出',2,NULL,NULL,46,0,'2025-07-01 08:00',N'system'),
(36,NULL,N'system:menu',N'系统管理',1,N'/System',N'fa-cog',50,0,'2025-07-01 08:00',N'system'),
(37,36,N'system:view',N'查看',2,NULL,NULL,51,0,'2025-07-01 08:00',N'system'),
(38,36,N'system:create',N'新增',2,NULL,NULL,52,0,'2025-07-01 08:00',N'system'),
(39,36,N'system:edit',N'编辑',2,NULL,NULL,53,0,'2025-07-01 08:00',N'system'),
(40,36,N'system:delete',N'删除',2,NULL,NULL,54,0,'2025-07-01 08:00',N'system'),
(41,36,N'system:approve',N'审批',2,NULL,NULL,55,0,'2025-07-01 08:00',N'system'),
(42,36,N'system:export',N'导出',2,NULL,NULL,56,0,'2025-07-01 08:00',N'system');
SET IDENTITY_INSERT Sys_Permission OFF;

SET IDENTITY_INSERT Sys_RolePermission ON;
-- admin(1): all 42 permissions
INSERT Sys_RolePermission(Id,RoleId,PermissionId) VALUES (1,1,1),(2,1,2),(3,1,3),(4,1,4),(5,1,5),(6,1,6),(7,1,7),(8,1,8),(9,1,9),(10,1,10),(11,1,11),(12,1,12),(13,1,13),(14,1,14),(15,1,15),(16,1,16),(17,1,17),(18,1,18),(19,1,19),(20,1,20),(21,1,21),(22,1,22),(23,1,23),(24,1,24),(25,1,25),(26,1,26),(27,1,27),(28,1,28),(29,1,29),(30,1,30),(31,1,31),(32,1,32),(33,1,33),(34,1,34),(35,1,35),(36,1,36),(37,1,37),(38,1,38),(39,1,39),(40,1,40),(41,1,41),(42,1,42);
-- manager(2): dashboard menu+approve+export, purchase/sale/stock menu+approve, maint menu
INSERT Sys_RolePermission(Id,RoleId,PermissionId) VALUES (43,2,1),(44,2,6),(45,2,7),(46,2,8),(47,2,13),(48,2,14),(49,2,15),(50,2,20),(51,2,21),(52,2,22),(53,2,27),(54,2,28),(55,2,29);
-- warehouse(3): purchase,stock menus + view/create/edit/export
INSERT Sys_RolePermission(Id,RoleId,PermissionId) VALUES (56,3,8),(57,3,9),(58,3,10),(59,3,11),(60,3,14),(61,3,22),(62,3,23),(63,3,24),(64,3,25),(65,3,28),(66,3,15),(67,3,16),(68,3,17),(69,3,18),(70,3,21);
-- sales(4): dashboard, sale menu + view/create/edit/export
INSERT Sys_RolePermission(Id,RoleId,PermissionId) VALUES (71,4,1),(72,4,15),(73,4,16),(74,4,17),(75,4,18),(76,4,21);
-- technician(5): maint menu + view/create/edit/export
INSERT Sys_RolePermission(Id,RoleId,PermissionId) VALUES (77,5,29),(78,5,30),(79,5,31),(80,5,32),(81,5,35);
SET IDENTITY_INSERT Sys_RolePermission OFF;

SET IDENTITY_INSERT Sys_Config ON;
INSERT Sys_Config(Id,ConfigKey,ConfigValue,ConfigName,Description,ConfigType,IsEditable,CreateTime,CreateBy) VALUES
(1,N'CompanyName',N'郑州恒锐自动化设备有限公司',N'公司名称',N'系统全局公司名称',1,0,'2025-07-01 08:00',N'system'),
(2,N'CompanyAddress',N'郑州市金水区花园路88号',N'公司地址',NULL,1,0,'2025-07-01 08:00',N'system'),
(3,N'LowStockThreshold',N'10',N'低库存预警阈值',N'库存低于此数量时触发预警',2,1,'2025-07-01 08:00',N'system'),
(4,N'SessionTimeout',N'30',N'会话超时(分钟)',NULL,2,1,'2025-07-01 08:00',N'system'),
(5,N'BackupPath',N'D:\Backup\IMS',N'数据库备份路径',NULL,1,1,'2025-07-01 08:00',N'system'),
(6,N'OrderPrefix',N'{YY}{MM}{DD}',N'单据编号日期格式',NULL,1,0,'2025-07-01 08:00',N'system'),
(7,N'AutoApproveAmount',N'5000',N'自动审批金额',N'低于此金额自动通过',2,1,'2025-07-01 08:00',N'system'),
(8,N'PageSize',N'20',N'默认分页大小',NULL,2,1,'2025-07-01 08:00',N'system'),
(9,N'NotificationEmail',N'admin@hengrauto.com',N'通知邮箱',NULL,1,1,'2025-07-01 08:00',N'system'),
(10,N'MinPasswordLength',N'8',N'密码最小长度',NULL,2,0,'2025-07-01 08:00',N'system');
SET IDENTITY_INSERT Sys_Config OFF;

SET IDENTITY_INSERT Sys_PasswordHistory ON;
INSERT Sys_PasswordHistory(Id,UserId,PasswordHash,PasswordSalt,CreateTime) VALUES
(1,1,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(2,2,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(3,3,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(4,4,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(5,5,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(6,6,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(7,7,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(8,8,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00'),(9,9,N'HASH_PLACE',N'SALT_PLACE','2025-07-01 08:00');
SET IDENTITY_INSERT Sys_PasswordHistory OFF;
GO

-- ===== 2. 基础模块 =====
SET IDENTITY_INSERT Base_Supplier ON;
INSERT Base_Supplier(Id,Code,Name,ContactPerson,ContactPhone,Address,BankAccount,BankName,IsDeleted,CreateTime,CreateBy) VALUES
(1,N'SUP-001',N'西门子（中国）有限公司郑州办',N'王建国',N'13837110001',N'郑东新区CBD商务外环路15号',N'ENC:6228480012345678910',N'中国农业银行',0,'2025-07-01 08:00',N'admin'),
(2,N'SUP-002',N'施耐德电气（中国）河南分公司',N'李志强',N'13837110002',N'金水区经三路66号',N'ENC:6228480012345678911',N'中国建设银行',0,'2025-07-01 08:00',N'admin'),
(3,N'SUP-003',N'三菱电机自动化郑州营业所',N'张明',N'13837110003',N'二七区大学路80号',N'ENC:6228480012345678912',N'中国工商银行',0,'2025-07-01 08:00',N'admin'),
(4,N'SUP-004',N'汇川技术股份有限公司河南办',N'陈晓东',N'13837110004',N'高新区科学大道100号',N'ENC:6228480012345678913',N'中国银行',0,'2025-07-01 08:00',N'admin'),
(5,N'SUP-005',N'台达电子企业管理郑州分公司',N'林伟',N'13837110005',N'管城区紫荆山路58号',N'ENC:6228480012345678914',N'招商银行',0,'2025-07-01 08:00',N'admin'),
(6,N'SUP-006',N'欧姆龙自动化（中国）郑州联络处',N'黄建华',N'13837110006',N'惠济区开元路120号',N'ENC:6228480012345678915',N'交通银行',0,'2025-07-01 08:00',N'admin'),
(7,N'SUP-007',N'松下电器机电（中国）郑州办',N'马丽',N'13837110007',N'中原区建设西路188号',N'ENC:6228480012345678916',N'中信银行',0,'2025-07-01 08:00',N'admin'),
(8,N'SUP-008',N'罗克韦尔自动化河南代表处',N'赵永刚',N'13837110008',N'经开区航海东路999号',N'ENC:6228480012345678917',N'浦发银行',0,'2025-07-01 08:00',N'admin');
SET IDENTITY_INSERT Base_Supplier OFF;

SET IDENTITY_INSERT Base_Customer ON;
INSERT Base_Customer(Id,Code,Name,ContactPerson,ContactPhone,Address,CreditLimit,UsedCredit,IsDeleted,CreateTime,CreateBy) VALUES
(1,N'CUS-001',N'郑州裕丰食品机械有限公司',N'刘建国',N'13526780001',N'新郑市龙湖镇食品工业园18号',80000,0,0,'2025-07-01 08:00',N'admin'),
(2,N'CUS-002',N'河南金汇建材科技有限公司',N'黄伟民',N'13526780002',N'巩义市永安路建材产业园55号',120000,0,0,'2025-07-01 08:00',N'admin'),
(3,N'CUS-003',N'新乡市新亚包装设备有限公司',N'郑志强',N'13526780003',N'新乡市红旗区新飞大道南段88号',60000,0,0,'2025-07-01 08:00',N'admin'),
(4,N'CUS-004',N'洛阳宏达机械制造厂',N'陈建国',N'13526780004',N'洛阳市涧西区建设路156号',100000,0,0,'2025-07-01 08:00',N'admin'),
(5,N'CUS-005',N'南阳恒通自动化工程有限公司',N'赵海龙',N'13526780005',N'南阳市卧龙区工业北路66号',50000,0,0,'2025-07-01 08:00',N'admin'),
(6,N'CUS-006',N'许昌瑞丰纸业设备有限公司',N'钱志明',N'13526780006',N'许昌市魏都区延安路128号',75000,0,0,'2025-07-01 08:00',N'admin'),
(7,N'CUS-007',N'开封天成粮油设备有限公司',N'孙建华',N'13526780007',N'开封市龙亭区宋城路99号',55000,0,0,'2025-07-01 08:00',N'admin'),
(8,N'CUS-008',N'焦作市华泰环保设备厂',N'周明辉',N'13526780008',N'焦作市山阳区建设东路200号',45000,0,0,'2025-07-01 08:00',N'admin'),
(9,N'CUS-009',N'安阳鑫达电线电缆有限公司',N'吴晓军',N'13526780009',N'安阳市北关区人民大道312号',35000,0,0,'2025-07-01 08:00',N'admin'),
(10,N'CUS-010',N'濮阳中原石油机械有限公司',N'李长江',N'13526780010',N'濮阳市华龙区中原路456号',150000,0,0,'2025-07-01 08:00',N'admin'),
(11,N'CUS-011',N'商丘市永泰纺织机械有限公司',N'王建军',N'13526780011',N'商丘市睢阳区南京路180号',65000,0,0,'2025-07-01 08:00',N'admin'),
(12,N'CUS-012',N'周口市富康生物科技有限公司',N'张海涛',N'13526780012',N'周口市川汇区太昊路88号',40000,0,0,'2025-07-01 08:00',N'admin'),
(13,N'CUS-013',N'驻马店市天中车辆配件厂',N'冯国栋',N'13526780013',N'驻马店市驿城区天中山大道266号',30000,0,0,'2025-07-01 08:00',N'admin'),
(14,N'CUS-014',N'信阳华英禽业集团设备部',N'何家明',N'13526780014',N'信阳市浉河区鸡公山大道18号',70000,0,0,'2025-07-01 08:00',N'admin'),
(15,N'CUS-015',N'三门峡金源矿业设备有限公司',N'徐卫东',N'13526780015',N'三门峡市湖滨区黄河路308号',85000,0,0,'2025-07-01 08:00',N'admin'),
(16,N'CUS-016',N'平顶山天安煤业设备科',N'高志强',N'13526780016',N'平顶山市新华区矿工路99号',200000,0,0,'2025-07-01 08:00',N'admin'),
(17,N'CUS-017',N'漯河双汇食品机械有限公司',N'林小龙',N'13526780017',N'漯河市召陵区人民东路100号',90000,0,0,'2025-07-01 08:00',N'admin'),
(18,N'CUS-018',N'鹤壁飞鹤建材有限公司',N'宋志远',N'13526780018',N'鹤壁市淇滨区兴鹤大街388号',55000,0,0,'2025-07-01 08:00',N'admin'),
(19,N'CUS-019',N'济源市太行冶金设备有限公司',N'唐国华',N'13526780019',N'济源市济水大街西段666号',48000,0,0,'2025-07-01 08:00',N'admin'),
(20,N'CUS-020',N'郑州恒宇电气工程有限公司',N'曹建国',N'13526780020',N'中原区桐柏路188号',50000,0,0,'2025-07-01 08:00',N'admin'),
(21,N'CUS-021',N'河南蓝天环保科技股份有限公司',N'杨晓峰',N'13526780021',N'高新区莲花街55号',60000,0,0,'2025-07-01 08:00',N'admin'),
(22,N'CUS-022',N'郑州东方机械制造有限公司',N'彭志强',N'13526780022',N'上街区汝南路77号',50000,0,0,'2025-07-01 08:00',N'admin'),
(23,N'CUS-023',N'郑州昌隆自动化设备有限公司',N'董明',N'13526780023',N'金水区农业路28号',50000,0,0,'2025-07-01 08:00',N'admin'),
(24,N'CUS-024',N'郑州鑫源电气有限公司',N'方海',N'13526780024',N'管城区航海路160号',50000,0,0,'2025-07-01 08:00',N'admin'),
(25,N'CUS-025',N'河南明泰铝业设备部',N'石英杰',N'13526780025',N'巩义市回郭镇开发区',80000,0,0,'2025-07-01 08:00',N'admin');
SET IDENTITY_INSERT Base_Customer OFF;

SET IDENTITY_INSERT Base_Category ON;
INSERT Base_Category(Id,ParentId,Code,Name,Level,SortOrder,IsDeleted,CreateTime,CreateBy) VALUES
(1,NULL,N'CAT-01',N'PLC控制器',1,1,0,'2025-07-01 08:00',N'admin'),
(2,NULL,N'CAT-02',N'变频器',1,2,0,'2025-07-01 08:00',N'admin'),
(3,NULL,N'CAT-03',N'伺服驱动器',1,3,0,'2025-07-01 08:00',N'admin'),
(4,NULL,N'CAT-04',N'传感器',1,4,0,'2025-07-01 08:00',N'admin'),
(5,NULL,N'CAT-05',N'工控机',1,5,0,'2025-07-01 08:00',N'admin'),
(6,NULL,N'CAT-06',N'低压电器',1,6,0,'2025-07-01 08:00',N'admin'),
(7,1,N'CAT-01-01',N'西门子PLC',2,1,0,'2025-07-01 08:00',N'admin'),
(8,2,N'CAT-02-01',N'通用变频器',2,1,0,'2025-07-01 08:00',N'admin'),
(9,3,N'CAT-03-01',N'交流伺服',2,1,0,'2025-07-01 08:00',N'admin'),
(10,4,N'CAT-04-01',N'光电传感器',2,1,0,'2025-07-01 08:00',N'admin'),
(11,5,N'CAT-05-01',N'嵌入式工控机',2,1,0,'2025-07-01 08:00',N'admin'),
(12,6,N'CAT-06-01',N'断路器',2,1,0,'2025-07-01 08:00',N'admin');
SET IDENTITY_INSERT Base_Category OFF;

SET IDENTITY_INSERT Base_Warehouse ON;
INSERT Base_Warehouse(Id,Code,Name,Address,Manager,Phone,IsDefault,IsDeleted,CreateTime,CreateBy) VALUES
(1,N'WH01',N'主仓库',N'金水区花园路88号B1',N'李坤',N'15003710003',1,0,'2025-07-01 08:00',N'admin'),
(2,N'WH02',N'维修仓库',N'金水区花园路88号3F维修中心',N'马翔',N'13303710008',0,0,'2025-07-01 08:00',N'admin'),
(3,N'WH03',N'分仓',N'高新区科学大道100号物流园A区',N'王芳',N'18603710004',0,0,'2025-07-01 08:00',N'admin');
SET IDENTITY_INSERT Base_Warehouse OFF;
GO
