using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IOperationLogService
{
    Task<PagedResult<OperationLogDto>> GetListAsync(OperationLogQueryDto query);
    Task<byte[]> ExportAsync(OperationLogQueryDto query);
    Task<List<OperationLogDto>> GetArchiveListAsync(OperationLogQueryDto query);
}
