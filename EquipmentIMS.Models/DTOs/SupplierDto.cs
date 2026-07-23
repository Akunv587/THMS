namespace EquipmentIMS.Models.DTOs;

public class SupplierDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountMasked { get; set; }
    public string? BankName { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
