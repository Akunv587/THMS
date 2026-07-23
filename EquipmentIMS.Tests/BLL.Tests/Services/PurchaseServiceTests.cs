using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;
using EquipmentIMS.Tests.TestData;

namespace EquipmentIMS.Tests.BLL.Tests.Services;

public class PurchaseServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPurchaseOrderRepository> _purOrderRepoMock;
    private readonly Mock<IRepository<PurPurchaseOrderDetail>> _detailRepoMock;
    private readonly Mock<IRepository<PurPurchaseIn>> _inRepoMock;
    private readonly Mock<IRepository<BaseProduct>> _productRepoMock;
    private readonly Mock<IStockRepository> _stocksMock;
    private readonly Mock<IStockTransactionRepository> _txnMock;
    private readonly Mock<INumberGeneratorService> _numberGenMock;
    private readonly Mock<INotificationService> _notifyMock;
    private readonly Mock<IDbContextTransaction> _txMock;
    private readonly AppDbContext _db;

    public PurchaseServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _purOrderRepoMock = new Mock<IPurchaseOrderRepository>();
        _detailRepoMock = new Mock<IRepository<PurPurchaseOrderDetail>>();
        _inRepoMock = new Mock<IRepository<PurPurchaseIn>>();
        _productRepoMock = new Mock<IRepository<BaseProduct>>();
        _stocksMock = new Mock<IStockRepository>();
        _txnMock = new Mock<IStockTransactionRepository>();
        _numberGenMock = new Mock<INumberGeneratorService>();
        _notifyMock = new Mock<INotificationService>();
        _txMock = new Mock<IDbContextTransaction>();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"PurchaseTest_{Guid.NewGuid():N}")
            .Options;
        _db = new AppDbContext(dbOptions);

        _uowMock.Setup(u => u.GetRepository<PurPurchaseOrderDetail>()).Returns(_detailRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<PurPurchaseIn>()).Returns(_inRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<BaseProduct>()).Returns(_productRepoMock.Object);
        _uowMock.SetupGet(u => u.Stocks).Returns(_stocksMock.Object);
        _uowMock.SetupGet(u => u.StockTransactions).Returns(_txnMock.Object);
        _uowMock.SetupGet(u => u.PurchaseOrders).Returns(_purOrderRepoMock.Object);
        _uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_txMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(() => _db.SaveChangesAsync());

        // Default mocks for repos used by InMemory-backed operations
        _uowMock.Setup(u => u.GetRepository<BaseProductStock>()).Returns(new MockDbRepo2<BaseProductStock>(_db));
        _uowMock.Setup(u => u.GetRepository<PurPurchaseInDetail>()).Returns(new MockDbRepo2<PurPurchaseInDetail>(_db));
        _uowMock.Setup(u => u.GetRepository<PurPurchaseReturn>()).Returns(new MockDbRepo2<PurPurchaseReturn>(_db));
        _uowMock.Setup(u => u.GetRepository<PurPurchaseReturnDetail>()).Returns(new MockDbRepo2<PurPurchaseReturnDetail>(_db));
        _uowMock.Setup(u => u.GetRepository<PurPurchaseOrder>()).Returns(Mock.Of<IRepository<PurPurchaseOrder>>());
    }

    private PurchaseService CreateSut() =>
        new(_uowMock.Object, _numberGenMock.Object, _notifyMock.Object, _db);

    private static BaseProductStock NewStock(long id, long productId, long warehouseId, int qty, int lockQty = 0)
        => new() { Id = id, ProductId = productId, WarehouseId = warehouseId, Quantity = qty, LockQty = lockQty, RowVersion = new byte[8] };

    #region PurchaseIn Tests

    [Fact]
    public void PurchaseIn_OrderNotApproved_Throws()
    {
        var order = new PurPurchaseOrder
        {
            Id = 1, OrderNo = "PUR-20260718-0001",
            Status = PurchaseOrderStatus.PendingApproval,
            IsDeleted = false
        };

        _purOrderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);

        var dto = new PurchaseInRequestDto
        {
            PurchaseOrderId = 1,
            Details = new List<PurchaseInItemRequestDto>()
        };

        var sut = CreateSut();

        var act = () => sut.PurchaseInAsync(dto, "user");
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*审批*");
    }

    [Fact]
    public async Task PurchaseIn_QtyExceed_Throws()
    {
        var order = new PurPurchaseOrder
        {
            Id = 1, OrderNo = "PUR-20260718-0001",
            Status = PurchaseOrderStatus.Approved,
            SupplierId = 1, WarehouseId = 1,
            IsDeleted = false
        };

        var product = TestDataGenerator.GenerateProducts(1)[0];
        product.Id = 1;

        // Add product to InMemory for .Include(d => d.Product) to resolve
        _db.BaseProducts.Add(product);

        var detail = new PurPurchaseOrderDetail
        {
            Id = 1, PurchaseOrderId = 1, ProductId = 1,
            Qty = 10, ReceivedQty = 5,
            UnitPrice = 100m
        };

        _db.PurPurchaseOrderDetails.Add(detail);
        await _db.SaveChangesAsync();

        // Point detailRepo mock to InMemory DB
        _detailRepoMock.Setup(r => r.Query()).Returns(_db.PurPurchaseOrderDetails.AsQueryable());

        _purOrderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.Query()).Returns(new[] { product }.AsQueryable());
        _numberGenMock.Setup(n => n.GenerateAsync("PIN", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("PIN-20260718-0001");

        var dto = new PurchaseInRequestDto
        {
            PurchaseOrderId = 1,
            Details = new List<PurchaseInItemRequestDto>
            {
                new() { ProductId = 1, Qty = 10, UnitPrice = 100m, Amount = 1000m, ProductCode = "DEV-0001" }
            }
        };

        var sut = CreateSut();

        var act = () => sut.PurchaseInAsync(dto, "user");
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*数量*");
    }

    [Fact]
    public async Task PurchaseIn_NormalFlow()
    {
        var product = TestDataGenerator.GenerateProducts(1)[0];
        product.Id = 1;
        _db.BaseProducts.Add(product);

        _db.BaseProductStocks.Add(NewStock(1, 1, 1, 10));

        var detail = new PurPurchaseOrderDetail
        {
            Id = 1, PurchaseOrderId = 1, ProductId = 1,
            Qty = 10, ReceivedQty = 0,
            UnitPrice = 100m
        };
        _db.PurPurchaseOrderDetails.Add(detail);
        await _db.SaveChangesAsync();

        _detailRepoMock.Setup(r => r.Query()).Returns(_db.PurPurchaseOrderDetails.AsQueryable());

        var order = new PurPurchaseOrder
        {
            Id = 1, OrderNo = "PUR-20260718-0001",
            Status = PurchaseOrderStatus.Approved,
            SupplierId = 1, WarehouseId = 1,
            IsDeleted = false
        };

        // Use InMemory-backed inRepo so AddAsync actually persists
        _uowMock.Setup(u => u.GetRepository<PurPurchaseIn>()).Returns(new MockDbRepo2<PurPurchaseIn>(_db));

        _purOrderRepoMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.Query()).Returns(new[] { product }.AsQueryable());
        _numberGenMock.Setup(n => n.GenerateAsync("PIN", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("PIN-20260718-0001");
        _numberGenMock.Setup(n => n.GenerateWithTimeAsync("TXN", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("TXN-20260718000001-001");
        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(1L, 1L))
            .ReturnsAsync(() => _db.BaseProductStocks.Find(1L));
        _txnMock.Setup(t => t.AddAsync(It.IsAny<InvStockTransaction>())).Returns(Task.CompletedTask);

        _notifyMock.Setup(n => n.SendAsync(It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationType>(), null, null))
            .Returns(Task.CompletedTask);

        var dto = new PurchaseInRequestDto
        {
            PurchaseOrderId = 1,
            Details = new List<PurchaseInItemRequestDto>
            {
                new() { ProductId = 1, Qty = 5, UnitPrice = 100m, Amount = 500m, ProductCode = "DEV-0001" }
            }
        };

        var sut = CreateSut();

        var result = await sut.PurchaseInAsync(dto, "user");

        result.Should().BeGreaterThan(0);

        var updatedDetail = await _db.PurPurchaseOrderDetails.FindAsync(1L);
        updatedDetail!.ReceivedQty.Should().Be(5);

        var updatedStock = await _db.BaseProductStocks.FindAsync(1L);
        updatedStock!.Quantity.Should().Be(15);
    }

    #endregion

    #region PurchaseReturn Tests

    [Fact]
    public async Task PurchaseReturn_NormalFlow()
    {
        var product = TestDataGenerator.GenerateProducts(1)[0];
        product.Id = 1;
        _db.BaseProducts.Add(product);

        _db.BaseProductStocks.Add(NewStock(1, 1, 1, 20));
        await _db.SaveChangesAsync();

        _numberGenMock.Setup(n => n.GenerateAsync("PRT", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("PRT-20260718-0001");
        _numberGenMock.Setup(n => n.GenerateWithTimeAsync("TXN", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("TXN-20260718000001-001");
        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(1L, 1L))
            .ReturnsAsync(() => _db.BaseProductStocks.Find(1L));
        _txnMock.Setup(t => t.AddAsync(It.IsAny<InvStockTransaction>())).Returns(Task.CompletedTask);

        var dto = new PurchaseReturnRequestDto
        {
            PurchaseInId = 1,
            Details = new List<PurchaseReturnItemRequestDto>
            {
                new() { ProductId = 1, Qty = 3, UnitPrice = 100m, Amount = 300m, ProductCode = "DEV-0001" }
            }
        };

        var sut = CreateSut();

        var act = () => sut.PurchaseReturnAsync(dto, "user");
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion
}

internal class MockDbRepo2<T> : IRepository<T> where T : BaseEntity
{
    public MockDbRepo2(DbContext db) => _db = db;
    private readonly DbContext _db;

    public IQueryable<T> Query() => _db.Set<T>().AsQueryable();
    public Task<T?> GetByIdAsync(long id) => _db.Set<T>().FindAsync(id).AsTask();
    public async Task<IEnumerable<T>> GetAllAsync() => await _db.Set<T>().ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _db.Set<T>().Where(predicate).ToListAsync();
    public async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null)
    {
        var q = predicate != null ? _db.Set<T>().Where(predicate) : _db.Set<T>().AsQueryable();
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
    public Task AddAsync(T entity)
    {
        // Auto-set RowVersion for InMemory [Timestamp] compatibility
        var rvProp = typeof(T).GetProperty("RowVersion");
        if (rvProp != null)
        {
            var val = rvProp.GetValue(entity) as byte[];
            if (val == null || val.Length == 0)
                rvProp.SetValue(entity, new byte[8]);
        }
        _db.Set<T>().Add(entity);
        return Task.CompletedTask;
    }
    public void Update(T entity) => _db.Set<T>().Update(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) => predicate != null ? await _db.Set<T>().CountAsync(predicate) : await _db.Set<T>().CountAsync();
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => await _db.Set<T>().AnyAsync(predicate);
}
