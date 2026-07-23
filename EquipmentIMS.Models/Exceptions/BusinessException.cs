namespace EquipmentIMS.Models.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; }

    public BusinessException(string message, string errorCode = "BIZ_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
