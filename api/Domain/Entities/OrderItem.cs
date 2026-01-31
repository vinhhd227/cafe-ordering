using api.Domain.Enums;

namespace api.Domain.Entities;

public class OrderItem : AuditableEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
    public DrinkTemperature? Temperature { get; set; }
    public IceLevel? IceLevel { get; set; }
    public SugarLevel? SugarLevel { get; set; }
}
