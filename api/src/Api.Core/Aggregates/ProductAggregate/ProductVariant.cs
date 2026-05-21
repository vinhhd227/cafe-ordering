namespace Api.Core.Aggregates.ProductAggregate;

public class ProductVariant : BaseEntity
{
  private readonly List<ProductVariantSelection> _values = new();

  private ProductVariant() { }

  public int ProductId { get; private set; }
  public decimal Price { get; private set; }
  public decimal? CostPrice { get; private set; }
  public string? Sku { get; private set; }
  public string? Barcode { get; private set; }
  public bool IsActive { get; private set; }
  public int DisplayOrder { get; private set; }

  public IReadOnlyCollection<ProductVariantSelection> Values => _values.AsReadOnly();

  internal static ProductVariant Create(
    int productId,
    decimal price,
    decimal? costPrice,
    string? sku,
    string? barcode,
    bool isActive,
    int displayOrder)
  {
    return new ProductVariant
    {
      ProductId = productId,
      Price = Guard.Against.Negative(price),
      CostPrice = costPrice,
      Sku = sku?.Trim(),
      Barcode = barcode?.Trim(),
      IsActive = isActive,
      DisplayOrder = displayOrder
    };
  }

  internal void AddValue(int optionValueId)
  {
    _values.Add(ProductVariantSelection.Create(optionValueId));
  }
}
