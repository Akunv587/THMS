namespace EquipmentIMS.Models.Exceptions;

public class ValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public ValidationException(Dictionary<string, string> errors)
        : base("参数校验失败")
    {
        Errors = errors;
    }
}
