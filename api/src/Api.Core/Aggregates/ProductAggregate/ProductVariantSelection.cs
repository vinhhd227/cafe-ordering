namespace Api.Core.Aggregates.ProductAggregate;

public class ProductVariantSelection : BaseEntity
{
  private ProductVariantSelection() { }

  public int ProductVariantId { get; private set; }
  public int ProductVariantValueId { get; private set; }

  public ProductVariant? ProductVariant { get; private set; }
  public ProductVariantValue? Value { get; private set; }

  internal static ProductVariantSelection Create(int optionValueId)
  {
    return new ProductVariantSelection
    {
      ProductVariantValueId = Guard.Against.NegativeOrZero(optionValueId)
    };
  }
}
