namespace Api.Core.Aggregates.ProductAggregate;

public class ProductVariantValue : BaseEntity
{
  private ProductVariantValue() { }

  public int GroupId { get; private set; }
  public string Label { get; private set; } = string.Empty;
  public decimal Price { get; private set; }
  public bool IsDefault { get; private set; }
  public int DisplayOrder { get; private set; }

  internal static ProductVariantValue Create(
    int groupId,
    string label,
    decimal price,
    bool isDefault,
    int displayOrder)
  {
    return new ProductVariantValue
    {
      GroupId = groupId,
      Label = Guard.Against.NullOrEmpty(label),
      Price = price,
      IsDefault = isDefault,
      DisplayOrder = displayOrder
    };
  }
}
