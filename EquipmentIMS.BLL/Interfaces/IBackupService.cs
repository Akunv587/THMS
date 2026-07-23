using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IBackupService
{
    Task<BackupResultDto> BackupAsync(string userId, string userName);
    Task<List<BackupHistoryDto>> GetBackupHistoryAsync(int page, int pageSize);
    Task<bool> VerifyBackupAsync(long historyId);
    Task<string?> GetBackupFilePathAsync(long id);
}
