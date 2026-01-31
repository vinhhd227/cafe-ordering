namespace api.Domain.Entities;

public class Product : AuditableEntity
{
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public bool HasTemperatureOption { get; set; }
    public bool HasIceLevelOption { get; set; }
    public bool HasSugarLevelOption { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
