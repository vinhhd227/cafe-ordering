namespace api.Domain.Entities;

public class Table : AuditableEntity
{
    public int Number { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
