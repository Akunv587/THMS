using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Tests.TestData;

namespace EquipmentIMS.Tests.BLL.Tests.Services;

public class MaintenanceServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRepository<MaintRepairOrder>> _orderRepoMock;
    private readonly Mock<IRepository<MaintRepairPart>> _partRepoMock;
    private readonly Mock<IRepository<BaseProductStock>> _stockRepoMock;
    private readonly Mock<IRepository<MaintDeviceArchive>> _archiveRepoMock;
    private readonly Mock<IStockRepository> _stockInterfaceMock;
    private readonly Mock<IStockTransactionRepository> _txnMock;
    private readonly Mock<INumberGeneratorService> _numberGenMock;
    private readonly Mock<IDbContextTransaction> _txMock;
    private readonly MaintenanceService _sut;

    public MaintenanceServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _orderRepoMock = new Mock<IRepository<MaintRepairOrder>>();
        _partRepoMock = new Mock<IRepository<MaintRepairPart>>();
        _stockRepoMock = new Mock<IRepository<BaseProductStock>>();
        _archiveRepoMock = new Mock<IRepository<MaintDeviceArchive>>();
        _stockInterfaceMock = new Mock<IStockRepository>();
        _txnMock = new Mock<IStockTransactionRepository>();
        _numberGenMock = new Mock<INumberGeneratorService>();
        _txMock = new Mock<IDbContextTransaction>();

        _uowMock.Setup(u => u.GetRepository<MaintRepairOrder>()).Returns(_orderRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<MaintRepairPart>()).Returns(_partRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<BaseProductStock>()).Returns(_stockRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<MaintDeviceArchive>()).Returns(_archiveRepoMock.Object);
        _uowMock.SetupGet(u => u.Stocks).Returns(_stockInterfaceMock.Object);
        _uowMock.SetupGet(u => u.StockTransactions).Returns(_txnMock.Object);
        _uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_txMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new MaintenanceService(_uowMock.Object, _numberGenMock.Object);
    }

    #region RepairOrder Status Transition Tests

    [Fact]
    public async Task RepairOrder_Create_DraftStatus()
    {
        // Arrange
        var product = TestDataGenerator.GenerateProducts(1)[0];
        var archive = new MaintDeviceArchive
        {
            Id = 1, ProductId = product.Id, SerialNo = "SN-001",
            Status = DeviceStatus.Normal, CustomerId = 1,
            IsDeleted = false
        };

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateRepair_{Guid.NewGuid():N}")
            .Options;
        var db = new AppDbContext(dbOptions);
        db.MaintDeviceArchives.Add(archive);
        await db.SaveChangesAsync();

        // Use InMemory-backed repo so AddAsync assigns an Id
        var orderRepo = new MockDbRepo<MaintRepairOrder>(db);
        _uowMock.Setup(u => u.GetRepository<MaintRepairOrder>()).Returns(orderRepo);
        _archiveRepoMock.Setup(r => r.Query()).Returns(db.MaintDeviceArchives.AsQueryable());

        _numberGenMock.Setup(n => n.GenerateAsync("RP", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("RP-20260718-0001");

        var dto = new RepairOrderDto
        {
            DeviceArchiveId = 1, CustomerId = 1,
            FaultDesc = "设备无法启动"
        };

        // Act
        var result = await _sut.CreateAsync(dto, "user");

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RepairOrder_StartRepair_ChangesStatus()
    {
        var order = new MaintRepairOrder
        {
            Id = 1, OrderNo = "RP-20260718-0001",
            Status = RepairOrderStatus.Dispatched,
            TechnicianId = 1, IsDeleted = false
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);

        await _sut.StartRepairAsync(1, "tech1");

        order.Status.Should().Be(RepairOrderStatus.InRepair);
        order.StartTime.Should().NotBeNull();
    }

    [Fact]
    public async Task RepairOrder_CompleteRepair_ChangesStatus()
    {
        var order = new MaintRepairOrder
        {
            Id = 1, OrderNo = "RP-20260718-0001",
            Status = RepairOrderStatus.InRepair,
            IsDeleted = false
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);

        await _sut.CompleteRepairAsync(1, "已修复，更换了主板", 2.5m, 300m, "tech1");

        order.Status.Should().Be(RepairOrderStatus.PendingConfirm);
        order.RepairDesc.Should().Be("已修复，更换了主板");
        order.WorkHours.Should().Be(2.5m);
        order.RepairCost.Should().Be(300m);
    }

    [Fact]
    public async Task RepairOrder_Cancel_RestoresStatus()
    {
        var archive = new MaintDeviceArchive
        {
            Id = 1, ProductId = 1, SerialNo = "SN-001",
            Status = DeviceStatus.InRepair, IsDeleted = false
        };

        var order = new MaintRepairOrder
        {
            Id = 1, OrderNo = "RP-20260718-0001",
            Status = RepairOrderStatus.InRepair,
            DeviceArchiveId = 1, IsDeleted = false
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);
        _archiveRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(archive);

        await _sut.CancelAsync(1, "user");

        order.Status.Should().Be(RepairOrderStatus.Cancelled);
        archive.Status.Should().Be(DeviceStatus.Normal);
    }

    #endregion

    #region Repair Parts Tests

    [Fact]
    public async Task RepairOrder_AddParts_DecreasesStock()
    {
        var stock = new BaseProductStock
        {
            Id = 1, ProductId = 100, WarehouseId = 1,
            Quantity = 50, LockQty = 0, RowVersion = new byte[8]
        };
        _stockInterfaceMock.Setup(s => s.FindByProductAndWarehouseAsync(100L, 1L))
            .ReturnsAsync(stock);

        var order = new MaintRepairOrder
        {
            Id = 1, OrderNo = "RP-20260718-0001",
            Status = RepairOrderStatus.InRepair,
            IsDeleted = false
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);
        _numberGenMock.Setup(n => n.GenerateAsync("ST", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("ST-20260718-0001");
        _txnMock.Setup(t => t.AddAsync(It.IsAny<InvStockTransaction>())).Returns(Task.CompletedTask);
        _partRepoMock.Setup(r => r.AddAsync(It.IsAny<MaintRepairPart>())).Returns(Task.CompletedTask);

        var parts = new List<RepairPartDto>
        {
            new() { ProductId = 100, WarehouseId = 1, Qty = 5, UnitPrice = 100m }
        };

        var act = () => _sut.RecordRepairPartAsync(1, parts, "tech1");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Maintenance Plan Tests

    [Fact]
    public async Task GetDuePlans_ReturnsExpiredPlans()
    {
        // Use a dedicated InMemory DB and MockDbRepo (from StockServiceTests.cs)
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DuePlans_{Guid.NewGuid():N}")
            .Options;
        var db = new AppDbContext(dbOptions);

        // Add DeviceArchive entities so .Include(e => e.DeviceArchive) works in InMemory
        db.MaintDeviceArchives.Add(new MaintDeviceArchive { Id = 1, ProductId = 1, SerialNo = "SN-001", IsDeleted = false });
        db.MaintDeviceArchives.Add(new MaintDeviceArchive { Id = 2, ProductId = 2, SerialNo = "SN-002", IsDeleted = false });
        db.MaintDeviceArchives.Add(new MaintDeviceArchive { Id = 3, ProductId = 3, SerialNo = "SN-003", IsDeleted = false });
        await db.SaveChangesAsync();

        var planRepo = new MockDbRepo<MaintMaintenancePlan>(db);
        await planRepo.AddAsync(new MaintMaintenancePlan { Id = 1, PlanName = "已到期计划", DeviceArchiveId = 1, NextDate = DateTime.Today.AddDays(-5), IsActive = true, IsDeleted = false });
        await planRepo.AddAsync(new MaintMaintenancePlan { Id = 2, PlanName = "未到期计划", DeviceArchiveId = 2, NextDate = DateTime.Today.AddDays(10), IsActive = true, IsDeleted = false });
        await planRepo.AddAsync(new MaintMaintenancePlan { Id = 3, PlanName = "已停用计划", DeviceArchiveId = 3, NextDate = DateTime.Today.AddDays(-3), IsActive = false, IsDeleted = false });
        await db.SaveChangesAsync();

        _uowMock.Setup(u => u.GetRepository<MaintMaintenancePlan>()).Returns(planRepo);

        var result = await _sut.GetDuePlansAsync();

        result.Should().HaveCount(1);
        result[0].PlanName.Should().Be("已到期计划");
    }

    #endregion
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<T>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executeMethod = typeof(IQueryProvider)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
            .MakeGenericMethod(expectedResultType);
        var executionResult = executeMethod.Invoke(_inner, new object[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}
