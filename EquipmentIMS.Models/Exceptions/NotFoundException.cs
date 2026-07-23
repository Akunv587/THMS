namespace EquipmentIMS.Models.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"未找到 {entityName}（Id={key}）")
    {
    }
}
