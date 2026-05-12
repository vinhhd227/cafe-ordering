using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate.Events;
using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.Core.Aggregates.ProductAggregate;

public class Product : SoftDeletableEntity<int>, IAggregateRoot
{
  private readonly List<ProductAttributeGroup> _attributeGroups = new();
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

  public IReadOnlyCollection<ProductAttributeGroup> AttributeGroups => _attributeGroups.AsReadOnly();
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
  ///   Xóa toàn bộ attribute groups hiện tại và thay bằng danh sách mới.
  ///   Gọi từ handler khi admin lưu cấu hình attribute.
  /// </summary>
  public void ReplaceAttributeGroups(IReadOnlyList<ProductAttributeGroupData> groups)
  {
    _attributeGroups.Clear();

    for (var i = 0; i < groups.Count; i++)
    {
      var g = groups[i];
      var group = ProductAttributeGroup.Create(Id, g.Name, g.IsRequired, g.SelectionType, i);

      for (var j = 0; j < g.Values.Count; j++)
      {
        var v = g.Values[j];
        group.AddValue(v.Label, v.PriceAdjustment, v.IsDefault, j);
      }

      _attributeGroups.Add(group);
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

  /// <summary>Gán một option group vào product. Bỏ qua nếu đã tồn tại.</summary>
  public void AssignOptionGroup(int groupId, int displayOrder)
  {
    if (_optionGroupMappings.Any(m => m.GroupId == groupId)) return;
    _optionGroupMappings.Add(ProductOptionGroupMapping.Create(Id, groupId, displayOrder));
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  /// <summary>Bỏ gán option group khỏi product.</summary>
  public void UnassignOptionGroup(int groupId)
  {
    var mapping = _optionGroupMappings.FirstOrDefault(m => m.GroupId == groupId);
    if (mapping is null) return;
    _optionGroupMappings.Remove(mapping);
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }

  /// <summary>Thay toàn bộ option group mappings (clear + recreate).</summary>
  public void ReplaceOptionGroupMappings(IReadOnlyList<int> groupIds)
  {
    _optionGroupMappings.Clear();
    for (var i = 0; i < groupIds.Count; i++)
      _optionGroupMappings.Add(ProductOptionGroupMapping.Create(Id, groupIds[i], i));
    RegisterDomainEvent(new ProductUpdatedEvent(this));
  }
}

/// <summary>Data transfer records cho ReplaceAttributeGroups.</summary>
public record ProductAttributeGroupData(
  string Name,
  bool IsRequired,
  OptionSelectionType SelectionType,
  IReadOnlyList<ProductAttributeValueData> Values);

public record ProductAttributeValueData(
  string Label,
  decimal PriceAdjustment,
  bool IsDefault);
