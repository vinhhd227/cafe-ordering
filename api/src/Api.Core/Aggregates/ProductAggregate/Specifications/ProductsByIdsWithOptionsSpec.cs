namespace Api.Core.Aggregates.ProductAggregate.Specifications;

[Obsolete("Use ProductsByIdsWithVariantGroupsSpec instead.")]
public class ProductsByIdsWithOptionsSpec : ProductsByIdsWithVariantGroupsSpec
{
  public ProductsByIdsWithOptionsSpec(IEnumerable<int> ids) : base(ids) { }
}
