using Bogus;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.Tests.TestData;

/// <summary>
/// 测试假数据生成器（Bogus）
/// </summary>
public static class TestDataGenerator
{
    static TestDataGenerator()
    {
        Randomizer.Seed = new Random(20260718);
    }

    public static Faker<SysUser> UserFaker => new Faker<SysUser>("zh_CN")
        .RuleFor(u => u.Id, f => f.Random.Long(1, 1000))
        .RuleFor(u => u.UserName, f => f.Person.UserName)
        .RuleFor(u => u.RealName, f => f.Person.FullName)
        .RuleFor(u => u.PasswordHash, f => f.Random.Hash())
        .RuleFor(u => u.IsActive, true)
        .RuleFor(u => u.IsDeleted, false)
        .RuleFor(u => u.MustChangePwd, false);

    public static Faker<BaseProduct> ProductFaker => new Faker<BaseProduct>("zh_CN")
        .RuleFor(p => p.Id, f => f.Random.Long(1, 10000))
        .RuleFor(p => p.Code, f => $"DEV-{f.Random.Number(1000, 9999)}")
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Spec, f => f.PickRandom("S7-1200", "V20", "6ES7", "ACS355", "标准型"))
        .RuleFor(p => p.Brand, f => f.PickRandom("西门子", "ABB", "施耐德", "三菱", "欧姆龙"))
        .RuleFor(p => p.Unit, f => f.PickRandom("个", "台", "套", "件"))
        .RuleFor(p => p.BarCode, f => $"BC{f.Random.Number(100000, 999999)}")
        .RuleFor(p => p.PurchasePrice, f => f.Finance.Amount(50, 5000))
        .RuleFor(p => p.SalePrice, f => f.Finance.Amount(80, 8000))
        .RuleFor(p => p.MinStock, f => f.Random.Int(5, 50))
        .RuleFor(p => p.IsDeleted, false);

    public static Faker<BaseCustomer> CustomerFaker => new Faker<BaseCustomer>("zh_CN")
        .RuleFor(c => c.Id, f => f.Random.Long(1, 500))
        .RuleFor(c => c.Code, f => $"CUST-{f.Random.Number(1000, 9999)}")
        .RuleFor(c => c.Name, f => f.Company.CompanyName())
        .RuleFor(c => c.ContactPerson, f => f.Person.FullName)
        .RuleFor(c => c.ContactPhone, f => f.Phone.PhoneNumber())
        .RuleFor(c => c.CreditLimit, f => f.Finance.Amount(10000, 100000))
        .RuleFor(c => c.IsDeleted, false);

    public static Faker<BaseSupplier> SupplierFaker => new Faker<BaseSupplier>("zh_CN")
        .RuleFor(s => s.Id, f => f.Random.Long(1, 200))
        .RuleFor(s => s.Code, f => $"SUPP-{f.Random.Number(1000, 9999)}")
        .RuleFor(s => s.Name, f => f.Company.CompanyName())
        .RuleFor(s => s.ContactPerson, f => f.Person.FullName)
        .RuleFor(s => s.ContactPhone, f => f.Phone.PhoneNumber())
        .RuleFor(s => s.IsDeleted, false);

    public static Faker<BaseWarehouse> WarehouseFaker => new Faker<BaseWarehouse>("zh_CN")
        .RuleFor(w => w.Id, f => f.Random.Long(1, 100))
        .RuleFor(w => w.Code, f => $"WH-{f.Random.Number(1, 99):D2}")
        .RuleFor(w => w.Name, f => f.PickRandom("主仓库", "备件仓库", "原材料仓", "成品仓", "维修备件仓"))
        .RuleFor(w => w.IsDeleted, false);

    public static Faker<BaseProductStock> ProductStockFaker => new Faker<BaseProductStock>("zh_CN")
        .RuleFor(s => s.Id, f => f.Random.Long(1, 10000))
        .RuleFor(s => s.ProductId, f => f.Random.Long(1, 10000))
        .RuleFor(s => s.WarehouseId, f => f.Random.Long(1, 100))
        .RuleFor(s => s.Quantity, f => f.Random.Int(10, 200))
        .RuleFor(s => s.LockQty, (f, s) => f.Random.Int(0, Math.Min(10, s.Quantity)));

    public static Faker<MaintMaintenancePlan> PlanFaker => new Faker<MaintMaintenancePlan>("zh_CN")
        .RuleFor(p => p.Id, f => f.Random.Long(1, 1000))
        .RuleFor(p => p.PlanName, f => f.PickRandom("月度保养", "季度保养", "年度大修", "日常巡检"))
        .RuleFor(p => p.CycleDays, f => f.Random.Int(30, 365))
        .RuleFor(p => p.NextDate, f => f.Date.Between(DateTime.Today.AddDays(-30), DateTime.Today))
        .RuleFor(p => p.IsActive, true)
        .RuleFor(p => p.IsDeleted, false);

    public static List<SysUser> GenerateUsers(int count = 5) => UserFaker.Generate(count);
    public static List<BaseProduct> GenerateProducts(int count = 10) => ProductFaker.Generate(count);
    public static List<BaseCustomer> GenerateCustomers(int count = 3) => CustomerFaker.Generate(count);
    public static List<BaseWarehouse> GenerateWarehouses(int count = 3) => WarehouseFaker.Generate(count);
    public static List<BaseProductStock> GenerateStocks(int count = 10) => ProductStockFaker.Generate(count);
    public static List<MaintMaintenancePlan> GeneratePlans(int count = 5) => PlanFaker.Generate(count);
}
