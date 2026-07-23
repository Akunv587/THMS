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

public class SaleServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ISaleOrderRepository> _saleOrdersMock;
    private readonly Mock<IRepository<SaleSaleOrderDetail>> _detailRepoMock;
    private readonly Mock<IRepository<BaseCustomer>> _customerRepoMock;
    private readonly Mock<IRepository<BaseProduct>> _productRepoMock;
    private readonly Mock<IRepository<SaleSaleOut>> _outRepoMock;
    private readonly Mock<IStockRepository> _stocksMock;
    private readonly Mock<IStockTransactionRepository> _txnMock;
    private readonly Mock<INumberGeneratorService> _numberGenMock;
    private readonly Mock<ISystemConfigService> _configMock;
    private readonly Mock<INotificationService> _notifyMock;
    private readonly Mock<IStockService> _stockServiceMock;
    private readonly Mock<IDbContextTransaction> _txMock;
    private readonly AppDbContext _db;
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _saleOrdersMock = new Mock<ISaleOrderRepository>();
        _detailRepoMock = new Mock<IRepository<SaleSaleOrderDetail>>();
        _customerRepoMock = new Mock<IRepository<BaseCustomer>>();
        _productRepoMock = new Mock<IRepository<BaseProduct>>();
        _outRepoMock = new Mock<IRepository<SaleSaleOut>>();
        _stocksMock = new Mock<IStockRepository>();
        _txnMock = new Mock<IStockTransactionRepository>();
        _numberGenMock = new Mock<INumberGeneratorService>();
        _configMock = new Mock<ISystemConfigService>();
        _notifyMock = new Mock<INotificationService>();
        _stockServiceMock = new Mock<IStockService>();
        _txMock = new Mock<IDbContextTransaction>();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"SaleTest_{Guid.NewGuid():N}")
            .Options;
        _db = new AppDbContext(dbOptions);

        _uowMock.Setup(u => u.GetRepository<SaleSaleOrderDetail>()).Returns(_detailRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<BaseCustomer>()).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<BaseProduct>()).Returns(_productRepoMock.Object);
        _uowMock.Setup(u => u.GetRepository<SaleSaleOut>()).Returns(_outRepoMock.Object);
        _uowMock.SetupGet(u => u.Stocks).Returns(_stocksMock.Object);
        _uowMock.SetupGet(u => u.StockTransactions).Returns(_txnMock.Object);
        _uowMock.SetupGet(u => u.SaleOrders).Returns(_saleOrdersMock.Object);
        _uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_txMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // For tests that pass a real InMemory db, ensure GetRepository for all types returns a usable mock
        _uowMock.Setup(u => u.GetRepository<SaleReturn>()).Returns(Mock.Of<IRepository<SaleReturn>>());
        _uowMock.Setup(u => u.GetRepository<SaleReturnDetail>()).Returns(Mock.Of<IRepository<SaleReturnDetail>>());
        _uowMock.Setup(u => u.GetRepository<BaseProductStock>()).Returns(Mock.Of<IRepository<BaseProductStock>>());
        _uowMock.Setup(u => u.GetRepository<SaleSaleOutDetail>()).Returns(Mock.Of<IRepository<SaleSaleOutDetail>>());
        _uowMock.Setup(u => u.GetRepository<SaleSaleOrder>()).Returns(Mock.Of<IRepository<SaleSaleOrder>>());

        _sut = new SaleService(_uowMock.Object, _numberGenMock.Object,
            _configMock.Object, _notifyMock.Object, _stockServiceMock.Object, _db);
    }

    private SaleSaleOrder CreateOrderWithDetails(
        long orderId = 1,
        SaleOrderStatus status = SaleOrderStatus.PendingApproval,
        long customerId = 1,
        decimal totalAmount = 1000m,
        params (long productId, long warehouseId, int qty, decimal unitPrice)[] details)
    {
        var order = new SaleSaleOrder
        {
            Id = orderId,
            OrderNo = $"SALE-20260718-{orderId:D4}",
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Status = status,
            IsDeleted = false,
            CreateBy = "testUser"
        };

        foreach (var d in details)
        {
            order.Details.Add(new SaleSaleOrderDetail
            {
                Id = order.Details.Count + 1,
                SaleOrderId = orderId,
                ProductId = d.productId,
                WarehouseId = d.warehouseId,
                Qty = d.qty,
                UnitPrice = d.unitPrice,
                Amount = d.qty * d.unitPrice,
                ShippedQty = 0
            });
        }
        return order;
    }

    #region SaleOrder Approve Tests

    [Fact]
    public void SaleOrder_Approve_InsufficientStock_Throws()
    {
        // Arrange
        var customer = TestDataGenerator.CustomerFaker.Generate();
        var product = TestDataGenerator.GenerateProducts(1)[0];

        var order = CreateOrderWithDetails(
            status: SaleOrderStatus.PendingApproval,
            customerId: customer.Id,
            details: new[] { (product.Id, 1L, 10, 100m) }
        );

        var stock = new BaseProductStock
        {
            Id = 1, ProductId = product.Id, WarehouseId = 1,
            Quantity = 5, LockQty = 2, RowVersion = new byte[8]
        };

        _saleOrdersMock.Setup(r => r.GetByIdWithDetailsAsync(1L)).ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.Query()).Returns(new[] { customer }.AsQueryable());
        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(product.Id, 1L))
            .ReturnsAsync(stock);
        _configMock.Setup(c => c.GetAsync("CreditCheckEnabled")).ReturnsAsync("false");

        // Act & Assert
        var act = () => _sut.ApproveAsync(1, "testUser");

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*库存*不足*");
    }

    [Fact]
    public async Task SaleOrder_Submit_InsufficientCredit_Throws()
    {
        // SubmitAsync calls CheckCreditAsync (unlike ApproveAsync). Use InMemory-backed
        // queryable so FirstOrDefaultAsync works inside the service.
        var custDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreditTest_{Guid.NewGuid():N}").Options);

        var customer = TestDataGenerator.CustomerFaker.Generate();
        customer.CreditLimit = 500m;
        customer.UsedCredit = 400m;
        custDb.BaseCustomers.Add(customer);
        await custDb.SaveChangesAsync();

        var product = TestDataGenerator.GenerateProducts(1)[0];
        var order = CreateOrderWithDetails(
            status: SaleOrderStatus.Draft,  // SubmitAsync requires Draft status
            customerId: customer.Id,
            totalAmount: 200m,  // 400 + 200 = 600 > 500 → credit exceeded
            details: new[] { (product.Id, 1L, 1, 200m) }
        );

        _saleOrdersMock.Setup(r => r.GetByIdWithDetailsAsync(1L)).ReturnsAsync(order);
        _saleOrdersMock.Setup(r => r.Update(order));
        _customerRepoMock.Setup(r => r.Query()).Returns(custDb.BaseCustomers.AsQueryable());
        _configMock.Setup(c => c.GetAsync("CreditCheckEnabled")).ReturnsAsync("true");

        // Act & Assert
        var act = () => _sut.SubmitAsync(1, "testUser");

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*信用*不足*");
    }

    [Fact]
    public async Task SaleOrder_Approve_LocksStock()
    {
        // Arrange
        var customer = TestDataGenerator.CustomerFaker.Generate();
        customer.CreditLimit = 100000m;
        customer.UsedCredit = 0;

        var product = TestDataGenerator.GenerateProducts(1)[0];
        var order = CreateOrderWithDetails(
            status: SaleOrderStatus.PendingApproval,
            customerId: customer.Id,
            totalAmount: 1000m,
            details: new[] { (product.Id, 1L, 5, 200m) }
        );

        var stock = new BaseProductStock
        {
            Id = 1, ProductId = product.Id, WarehouseId = 1,
            Quantity = 100, LockQty = 20, RowVersion = new byte[8]
        };

        _saleOrdersMock.Setup(r => r.GetByIdWithDetailsAsync(1L)).ReturnsAsync(order);
        _customerRepoMock.Setup(r => r.Query()).Returns(new[] { customer }.AsQueryable());
        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(product.Id, 1L))
            .ReturnsAsync(stock);
        _configMock.Setup(c => c.GetAsync("CreditCheckEnabled")).ReturnsAsync("false");

        // Act
        await _sut.ApproveAsync(1, "approver");

        // Assert
        stock.LockQty.Should().Be(25);
        order.Status.Should().Be(SaleOrderStatus.Approved);
    }

    #endregion

    #region SaleOut Tests

    [Fact]
    public void SaleOut_StockNotFound_Throws()
    {
        // Arrange
        var dto = new SaleOutDto
        {
            SaleOrderId = 1,
            WarehouseId = 1,
            Details = new List<SaleOutDetailDto>
            {
                new() { ProductId = 999, Qty = 5, UnitPrice = 100m }
            }
        };

        _stockServiceMock.Setup(s => s.IsProductInActiveCheckAsync(999)).ReturnsAsync(false);

        // Act & Assert
        var act = () => _sut.SaleOutAsync(dto, "user");

        act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SaleOut_InsufficientStock_Throws()
    {
        // Arrange
        var product = TestDataGenerator.GenerateProducts(1)[0];

        var order = CreateOrderWithDetails(
            status: SaleOrderStatus.Approved,
            details: new[] { (product.Id, 1L, 10, 100m) }
        );

        _saleOrdersMock.Setup(r => r.GetByIdWithDetailsAsync(1L)).ReturnsAsync(order);

        var stock = new BaseProductStock
        {
            Id = 1, ProductId = product.Id, WarehouseId = 1,
            Quantity = 3, LockQty = 0, RowVersion = new byte[8]
        };
        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(product.Id, 1L))
            .ReturnsAsync(stock);
        _stockServiceMock.Setup(s => s.IsProductInActiveCheckAsync(product.Id)).ReturnsAsync(false);

        var dto = new SaleOutDto
        {
            SaleOrderId = 1, WarehouseId = 1,
            Details = new List<SaleOutDetailDto>
            {
                new() { ProductId = product.Id, Qty = 10, UnitPrice = 100m, SaleOrderDetailId = 1 }
            }
        };

        // Act & Assert
        var act = () => _sut.SaleOutAsync(dto, "user");
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region SaleReturn Tests

    [Fact]
    public async Task SaleReturn_Approve_StockIncreases()
    {
        // Arrange
        var product = TestDataGenerator.GenerateProducts(1)[0];

        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ReturnTest_{Guid.NewGuid():N}").Options);

        // Add ALL entities needed for the Include chains in ApproveReturnAsync:
        //   .Include(r => r.Details).ThenInclude(d => d.Product)
        //   .Include(r => r.SaleOut).ThenInclude(o => o.SaleOrder)
        // InMemory EF Core treats Include as inner join, so every navigation must exist.

        db.BaseProducts.Add(product);

        var saleOrder = new SaleSaleOrder
        {
            Id = 1, OrderNo = "SALE-20260718-0001",
            CustomerId = 1, Status = SaleOrderStatus.Approved,
            IsDeleted = false
        };
        db.SaleSaleOrders.Add(saleOrder);

        var saleOut = new SaleSaleOut
        {
            Id = 1, OutNo = "SO-20260718-0001",
            SaleOrderId = 1, WarehouseId = 1,
            Status = (int)SaleOutStatus.Out,
            IsDeleted = false
        };
        db.SaleSaleOuts.Add(saleOut);

        var stock = new BaseProductStock
        {
            Id = 1, ProductId = product.Id, WarehouseId = 1,
            Quantity = 50, LockQty = 10, RowVersion = new byte[8]
        };
        db.BaseProductStocks.Add(stock);

        var returnOrder = new SaleReturn
        {
            Id = 1, ReturnNo = "SR-20260718-0001",
            SaleOutId = 1, CustomerId = 1, WarehouseId = 1,
            Status = SaleReturnStatus.PendingApproval,
            IsDeleted = false
        };

        var returnDetail = new SaleReturnDetail
        {
            Id = 1, SaleReturnId = 1, ProductId = product.Id,
            Qty = 3, WarehouseId = 1, SaleOutDetailId = 1
        };

        db.SaleReturns.Add(returnOrder);
        db.SaleReturnDetails.Add(returnDetail);
        await db.SaveChangesAsync();

        // Mock the repos used by a dedicated Sut
        var uowMock2 = new Mock<IUnitOfWork>();
        uowMock2.Setup(u => u.GetRepository<SaleReturn>())
            .Returns(new MockDbRepository<SaleReturn>(db));
        uowMock2.Setup(u => u.GetRepository<SaleReturnDetail>())
            .Returns(new MockDbRepository<SaleReturnDetail>(db));
        uowMock2.Setup(u => u.GetRepository<BaseProductStock>())
            .Returns(new MockDbRepository<BaseProductStock>(db));
        uowMock2.Setup(u => u.GetRepository<SaleSaleOrderDetail>()).Returns(_detailRepoMock.Object);
        uowMock2.Setup(u => u.GetRepository<SaleSaleOrder>()).Returns(Mock.Of<IRepository<SaleSaleOrder>>());
        uowMock2.Setup(u => u.GetRepository<BaseCustomer>()).Returns(_customerRepoMock.Object);
        uowMock2.Setup(u => u.GetRepository<BaseProduct>()).Returns(_productRepoMock.Object);
        uowMock2.Setup(u => u.GetRepository<SaleSaleOut>()).Returns(_outRepoMock.Object);
        uowMock2.Setup(u => u.GetRepository<SaleSaleOutDetail>()).Returns(Mock.Of<IRepository<SaleSaleOutDetail>>());
        uowMock2.SetupGet(u => u.Stocks).Returns(_stocksMock.Object);
        uowMock2.SetupGet(u => u.StockTransactions).Returns(_txnMock.Object);
        uowMock2.SetupGet(u => u.SaleOrders).Returns(_saleOrdersMock.Object);
        uowMock2.Setup(u => u.SaveChangesAsync()).Returns(() => db.SaveChangesAsync());

        _stocksMock.Setup(s => s.FindByProductAndWarehouseAsync(product.Id, 1L))
            .ReturnsAsync(() => db.BaseProductStocks.Find(1L));
        _txnMock.Setup(t => t.AddAsync(It.IsAny<InvStockTransaction>())).Returns(Task.CompletedTask);
        _notifyMock.Setup(n => n.SendAsync(It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationType>(), null, null))
            .Returns(Task.CompletedTask);

        var sutWithDb = new SaleService(uowMock2.Object, _numberGenMock.Object,
            _configMock.Object, _notifyMock.Object, _stockServiceMock.Object, db);

        // Act
        await sutWithDb.ApproveReturnAsync(1, "approver");

        // Assert
        var updatedStock = await db.BaseProductStocks.FindAsync(1L);
        updatedStock!.Quantity.Should().Be(53);
    }

    #endregion
}

/// <summary>
/// Simple IRepository wrapper that reads/writes directly from an EF Core DbContext for testing.
/// </summary>
internal class MockDbRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly DbContext _db;
    public MockDbRepository(DbContext db) => _db = db;

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
    public Task AddAsync(T entity) { _db.Set<T>().Add(entity); return Task.CompletedTask; }
    public void Update(T entity) => _db.Set<T>().Update(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate != null ? await _db.Set<T>().CountAsync(predicate) : await _db.Set<T>().CountAsync();
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await _db.Set<T>().AnyAsync(predicate);
}
