using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate.Events;
using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.Core.Aggregates.ProductAggregate;

public class Product : SoftDeletableEntity<int>, IAggregateRoot
{
  private readonly List<ProductVariantGroup> _variantGroups = new();
  private readonly List<ProductVariant> _variants = new();
  private readonly List<ProductOptionGroupMapping> _optionGroupMappings = new();

  private Product() { }

  public int? CategoryId { get; private set; }
  public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public decimal Price { get; private set; }
  public decimal? CostPrice { get; private set; }
  public decimal? DiscountPrice { get; private set; }
  public string? Sku { get; private set; }
  public string? Barcode { get; private set; }
  public bool IsActive { get; private set; } = true;
  public string? ImageUrl { get; private set; }
  public bool IsAccompaniment { get; private set; }
  public int? EstimatedPrepMinutes { get; private set; }

  public IReadOnlyCollection<ProductVariantGroup> VariantGroups => _variantGroups.AsReadOnly();
  public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
  public IReadOnlyCollection<ProductOptionGroupMapping> OptionGroupMappings => _optionGroupMappings.AsReadOnly();

  // Navigation
  public Category? Category { get; private set; }

  public static Product Create(
    string name,
    decimal price,
    int? categoryId = null,
    string? description = null,
    string? imageUrl = null,
    bool isAccompaniment = false,
    decimal? costPrice = null,
    decimal? discountPrice = null,
    string? sku = null,
    string? barcode = null)
  {
    var product = new Product
    {
      CategoryId = categoryId > 0 ? categoryId : null,
      Name = Guard.Against.NullOrEmpty(name),
      Price = Guard.Against.Negative(price),
      Description = description,
      ImageUrl = imageUrl,
      IsAccompaniment = isAccompaniment,
      IsActive = true,
      CostPrice = costPrice,
      DiscountPrice = discountPrice,
      Sku = sku?.Trim(),
      Barcode = barcode?.Trim(),
    };

    product.RegisterDomainEvent(new ProductCreatedEvent(product));

    return product;
  }

  public void UpdateAccompaniment(bool value) => IsAccompaniment = value;

  public void SetEstimatedPrepTime(int? minutes) => EstimatedPrepMinutes = minutes;

  public void SetCostPrice(decimal? value) => CostPrice = value;

  public void SetDiscountPrice(decimal? value) => DiscountPrice = value;

  public void SetSku(string? value) => Sku = value?.Trim();

  public void SetBarcode(string? value) => Barcode = value?.Trim();

  public void UpdateDetails(
    string name,
    decimal price,
    string? description = null,
    string? imageUrl = null)
  {
    Name = Guard.Against.NullOrEmpty(name);
    Price = Guard.Against.Negative(price);
    Description = description;
    ImageUrl = imageUrl;

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  /// <summary>
  ///   XÃ³a toÃ n bá»™ variant option groups hiá»‡n táº¡i vÃ  thay báº±ng danh sÃ¡ch má»›i.
  ///   Gá»i tá»« handler khi admin lÆ°u cáº¥u hÃ¬nh variant option.
  /// </summary>
  public void ReplaceVariantGroups(IReadOnlyList<ProductVariantGroupData> groups)
  {
    _variantGroups.Clear();
    _variants.Clear();

    for (var i = 0; i < groups.Count; i++)
    {
      var g = groups[i];
      var group = ProductVariantGroup.Create(Id, g.Name, g.IsRequired, g.SelectionType, i);

      for (var j = 0; j < g.Values.Count; j++)
      {
        var v = g.Values[j];
        group.AddValue(v.Label, v.Price, v.IsDefault, j);
      }

      _variantGroups.Add(group);
    }

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void ReplaceVariants(IReadOnlyList<ProductVariantData> variants)
  {
    _variants.Clear();

    for (var i = 0; i < variants.Count; i++)
    {
      var v = variants[i];
      var variant = ProductVariant.Create(
        Id,
        v.Price,
        v.CostPrice,
        v.Sku,
        v.Barcode,
        v.IsActive,
        i);

      foreach (var valueId in v.ValueIds.Distinct().OrderBy(id => id))
        variant.AddValue(valueId);

      _variants.Add(variant);
    }

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void ChangeCategory(int? categoryId)
  {
    CategoryId = categoryId > 0 ? categoryId : null;

    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  public void Activate()
  {
    IsActive = true;

    RegisterDomainEvent(new ProductActivatedEvent(Id));
  }

  public void Deactivate()
  {
    IsActive = false;

    RegisterDomainEvent(new ProductDeactivatedEvent(Id));
  }

  /// <summary>GÃ¡n má»™t option group vÃ o product. Bá» qua náº¿u Ä‘Ã£ tá»“n táº¡i.</summary>
  public void AssignOptionGroup(int groupId, int displayOrder)
  {
    if (_optionGroupMappings.Any(m => m.GroupId == groupId)) return;
    _optionGroupMappings.Add(ProductOptionGroupMapping.Create(Id, groupId, displayOrder));
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  /// <summary>Bá» gÃ¡n option group khá»i product.</summary>
  public void UnassignOptionGroup(int groupId)
  {
    var mapping = _optionGroupMappings.FirstOrDefault(m => m.GroupId == groupId);
    if (mapping is null) return;
    _optionGroupMappings.Remove(mapping);
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  /// <summary>Thay toÃ n bá»™ option group mappings (clear + recreate).</summary>
  public void ReplaceOptionGroupMappings(IReadOnlyList<int> groupIds)
  {
    _optionGroupMappings.Clear();
    for (var i = 0; i < groupIds.Count; i++)
      _optionGroupMappings.Add(ProductOptionGroupMapping.Create(Id, groupIds[i], i));
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }
}

/// <summary>Data transfer records cho ReplaceVariantGroups.</summary>
public record ProductVariantGroupData(
  string Name,
  bool IsRequired,
  OptionSelectionType SelectionType,
  IReadOnlyList<ProductVariantValueData> Values);

public record ProductVariantValueData(
  string Label,
  decimal Price,
  bool IsDefault);

public record ProductVariantData(
  IReadOnlyList<int> ValueIds,
  decimal Price,
  decimal? CostPrice,
  string? Sku,
  string? Barcode,
  bool IsActive);
