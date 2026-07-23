using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class ProductService : IProductService
{
    private readonly IRepository<BaseProduct> _repo;
    private readonly ICategoryService _categoryService;
    private readonly ICacheService _cache;

    public ProductService(IUnitOfWork uow, ICategoryService categoryService, ICacheService cache)
    {
        _repo = uow.GetRepository<BaseProduct>();
        _categoryService = categoryService;
        _cache = cache;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, string? keyword = null, long? categoryId = null)
    {
        System.Linq.Expressions.Expression<Func<BaseProduct, bool>>? predicate = p => !p.IsDeleted;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword;
            predicate = p => !p.IsDeleted && (p.Code.Contains(kw) || p.Name.Contains(kw) || (p.Spec != null && p.Spec.Contains(kw)));
        }
        if (categoryId.HasValue)
        {
            var cid = categoryId.Value;
            var prev = predicate;
            predicate = p => prev.Compile()(p) && p.CategoryId == cid;
        }

        var result = await _repo.GetPagedAsync(page, pageSize, predicate);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(long id)
    {
        var entity = await _repo.Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ProductDto?> FindByBarCodeAsync(string barCode)
    {
        if (string.IsNullOrWhiteSpace(barCode)) return null;
        var entity = await _repo.Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.BarCode == barCode && !p.IsDeleted);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<ProductDto>> GetAllAsync(string? keyword = null)
    {
        var query = _repo.Query().Include(p => p.Category).Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword));
        var entities = await query.OrderBy(p => p.Code).Take(200).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<ProductDto> CreateAsync(ProductDto dto)
    {
        var entity = new BaseProduct
        {
            Code = dto.Code,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Spec = dto.Spec,
            Brand = dto.Brand,
            Unit = dto.Unit,
            BarCode = dto.BarCode,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            MinStock = dto.MinStock,
            Remark = dto.Remark
        };
        await _repo.AddAsync(entity);
        return MapToDto(entity);
    }

    public async Task UpdateAsync(ProductDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.CategoryId = dto.CategoryId;
        entity.Spec = dto.Spec;
        entity.Brand = dto.Brand;
        entity.Unit = dto.Unit;
        entity.BarCode = dto.BarCode;
        entity.PurchasePrice = dto.PurchasePrice;
        entity.SalePrice = dto.SalePrice;
        entity.MinStock = dto.MinStock;
        entity.Remark = dto.Remark;
        _repo.Update(entity);
    }

    public async Task DeleteAsync(long id)
    {
        var hasStock = await _repo.Query().AnyAsync(p => p.Id == id && p.ProductStocks.Any(s => !s.IsDeleted));
        if (hasStock) throw new InvalidOperationException("该设备存在库存，无法删除");
        await _repo.Query().Where(p => p.Id == id && !p.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var query = _repo.Query().Where(p => !p.IsDeleted && p.Code == code);
        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<ImportResult> ImportExcelAsync(IFormFile file)
    {
        var result = new ImportResult();
        var categories = await _categoryService.GetTreeAsync();
        var catMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        void Flatten(List<CategoryTreeDto> nodes)
        {
            foreach (var n in nodes) { catMap[n.Name] = n.Id; Flatten(n.Children); }
        }
        Flatten(categories);

        using var stream = file.OpenReadStream();
        var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);

        for (int row = 1; row <= sheet.LastRowNum; row++)
        {
            var r = sheet.GetRow(row);
            if (r == null) continue;
            result.TotalCount++;

            var code = GetCellString(r, 0);
            var name = GetCellString(r, 1);
            var catName = GetCellString(r, 2);
            var spec = GetCellString(r, 3);
            var brand = GetCellString(r, 4);
            var unit = GetCellString(r, 5);
            var barcode = GetCellString(r, 6);
            var purchasePrice = GetCellDecimal(r, 7);
            var salePrice = GetCellDecimal(r, 8);
            var minStock = (int)GetCellDecimal(r, 9);

            if (string.IsNullOrWhiteSpace(code))
            {
                result.FailItems.Add(new ImportFailItem { Row = row + 1, Reason = "编码不能为空" });
                result.FailCount++;
                continue;
            }
            if (await CodeExistsAsync(code))
            {
                result.FailItems.Add(new ImportFailItem { Row = row + 1, Code = code, Reason = "编码已存在" });
                result.FailCount++;
                continue;
            }
            if (!catMap.TryGetValue(catName ?? "", out var catId))
            {
                result.FailItems.Add(new ImportFailItem { Row = row + 1, Code = code, Reason = $"分类'{catName}'不存在" });
                result.FailCount++;
                continue;
            }

            await CreateAsync(new ProductDto
            {
                Code = code!, Name = name, CategoryId = catId, Spec = spec, Brand = brand,
                Unit = unit, BarCode = barcode, PurchasePrice = purchasePrice,
                SalePrice = salePrice, MinStock = minStock
            });
            result.SuccessCount++;
        }

        await _cache.RemoveAsync("CategoryTree");
        return result;
    }

    private static string? GetCellString(IRow row, int col)
    {
        var cell = row.GetCell(col);
        if (cell == null) return null;
        return cell.CellType switch
        {
            CellType.Numeric => cell.NumericCellValue.ToString(),
            CellType.String => cell.StringCellValue.Trim(),
            _ => cell.ToString()?.Trim()
        };
    }

    private static decimal GetCellDecimal(IRow row, int col)
    {
        var cell = row.GetCell(col);
        if (cell == null) return 0;
        if (cell.CellType == CellType.Numeric) return (decimal)cell.NumericCellValue;
        return decimal.TryParse(cell.ToString(), out var v) ? v : 0;
    }

    private static ProductDto MapToDto(BaseProduct e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name, CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? "", Spec = e.Spec, Brand = e.Brand,
        Unit = e.Unit, BarCode = e.BarCode, PurchasePrice = e.PurchasePrice,
        SalePrice = e.SalePrice, MinStock = e.MinStock, Remark = e.Remark, CreateTime = e.CreateTime
    };
}
