namespace Api.Core.Aggregates.ProductAggregate;

public class ProductAttributeValue : BaseEntity
{
  private ProductAttributeValue() { }

  public int GroupId { get; private set; }
  public string Label { get; private set; } = string.Empty;
  public decimal PriceAdjustment { get; private set; }
  public bool IsDefault { get; private set; }
  public int DisplayOrder { get; private set; }

  internal static ProductAttributeValue Create(
    int groupId,
    string label,
    decimal priceAdjustment,
    bool isDefault,
    int displayOrder)
  {
    return new ProductAttributeValue
    {
      GroupId = groupId,
      Label = Guard.Against.NullOrEmpty(label),
      PriceAdjustment = priceAdjustment,
      IsDefault = isDefault,
      DisplayOrder = displayOrder
    };
  }
}
