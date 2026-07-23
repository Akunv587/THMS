namespace EquipmentIMS.BLL.Interfaces;

/// <summary>
/// 编号生成服务接口
/// </summary>
public interface INumberGeneratorService
{
    /// <summary>
    /// 生成业务编号（格式: PREFIX-YYYYMMDD-NNNN）
    /// </summary>
    /// <param name="prefix">编号前缀</param>
    /// <param name="tableName">查询已有编号的表名</param>
    /// <param name="columnName">编号列名</param>
    Task<string> GenerateAsync(string prefix, string tableName, string columnName);

    /// <summary>
    /// 生成带时序的编号（库存流水格式: PREFIX-YYYYMMDDHHmmss-NNN）
    /// </summary>
    Task<string> GenerateWithTimeAsync(string prefix, string tableName, string columnName);
}
