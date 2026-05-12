namespace Api.Core.Aggregates.ProductAggregate.Specifications;

[Obsolete("Use ProductsByIdsWithAttributesSpec instead.")]
public class ProductsByIdsWithOptionsSpec : ProductsByIdsWithAttributesSpec
{
  public ProductsByIdsWithOptionsSpec(IEnumerable<int> ids) : base(ids) { }
}
