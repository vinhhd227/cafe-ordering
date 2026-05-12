namespace Api.Core.Aggregates.ProductOptionGroupAggregate;

/// <summary>
///   Junction entity: Product ↔ ProductOptionGroup (many-to-many).
///   Tách explicit để lưu DisplayOrder.
/// </summary>
public class ProductOptionGroupMapping : BaseEntity
{
  private ProductOptionGroupMapping() { }

  public int ProductId { get; private set; }
  public int GroupId { get; private set; }
  public int DisplayOrder { get; private set; }

  // Navigation property — populated when using .Include()
  public ProductOptionGroup? Group { get; private set; }

  public static ProductOptionGroupMapping Create(int productId, int groupId, int displayOrder)
  {
    return new ProductOptionGroupMapping
    {
      ProductId = productId,
      GroupId = groupId,
      DisplayOrder = displayOrder,
    };
  }

  internal void SetDisplayOrder(int order) => DisplayOrder = order;
}
