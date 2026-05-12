namespace Api.Core.Aggregates.ProductOptionGroupAggregate;

public class ProductOptionValue : BaseEntity
{
  private ProductOptionValue() { }

  public int GroupId { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public decimal Price { get; private set; }
  public decimal? CostPrice { get; private set; }
  public bool IsInStock { get; private set; } = true;
  public int DisplayOrder { get; private set; }

  internal static ProductOptionValue Create(
    string name,
    decimal price,
    decimal? costPrice,
    int displayOrder)
  {
    return new ProductOptionValue
    {
      GroupId = 0, // EF Core fixup khi AddAsync
      Name = Guard.Against.NullOrWhiteSpace(name),
      Price = Guard.Against.Negative(price),
      CostPrice = costPrice,
      IsInStock = true,
      DisplayOrder = displayOrder,
    };
  }

  internal void SetStock(bool inStock) => IsInStock = inStock;
}
