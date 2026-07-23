namespace EquipmentIMS.Models.DTOs;

public class CategoryDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int SortOrder { get; set; }
}

public class CategoryTreeDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = new();
}
