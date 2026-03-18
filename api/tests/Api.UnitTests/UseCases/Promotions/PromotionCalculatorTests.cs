using System.Reflection;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.PromotionAggregate;
using Api.UseCases.Promotions.Apply;

namespace Api.UnitTests.UseCases.Promotions;

public class PromotionCalculatorTests
{
  private static readonly DateTime ValidStart = DateTime.UtcNow.AddDays(-1);

  // Creates an Order with a non-zero Id so AddItem passes the guard
  private static Order CreateOrder(int id = 1)
  {
    var order = Order.Create(Guid.NewGuid(), "ORD-TEST");
    typeof(Order).GetProperty("Id")!.GetSetMethod(nonPublic: true)!.Invoke(order, [id]);
    return order;
  }

  private static Promotion CreateBuyXGetY(
    int buyQty, int getQty,
    PromotionScope? scope = null,
    List<int>? applicableProductIds = null,
    List<int>? getFromProductIds = null)
    => Promotion.Create(
      name: "Test BuyXGetY",
      code: "TEST",
      codeVisibility: CodeVisibility.Public,
      description: null,
      discountType: DiscountType.BuyXGetY,
      discountValue: 0,
      maxDiscountAmount: null,
      buyQuantity: buyQty,
      getQuantity: getQty,
      scope: scope ?? PromotionScope.Order,
      applicableProductIds: applicableProductIds,
      applicableCategoryIds: null,
      getFromProductIds: getFromProductIds,
      getFromCategoryIds: null,
      minOrderAmount: null,
      startDate: ValidStart,
      endDate: null,
      maxUsage: null);

  // ── Buy 2 Get 1 (Order scope) ──────────────────────────────────────

  [Fact]
  public void Buy2Get1_OrderScope_With2Items_ShouldGiveCheapestFree()
  {
    var order = CreateOrder();
    order.AddItem(1, "Cà phê", 35_000, 1);
    order.AddItem(2, "Trà sữa", 45_000, 1);

    var promo = CreateBuyXGetY(buyQty: 2, getQty: 1);
    var result = PromotionCalculator.Calculate(promo, order.Items);

    result.TotalDiscount.Should().Be(35_000); // cheapest = Cà phê
  }

  [Fact]
  public void Buy2Get1_OrderScope_With4Items_ShouldGive2Free()
  {
    var order = CreateOrder();
    order.AddItem(1, "Cà phê", 35_000, 2);
    order.AddItem(2, "Trà sữa", 45_000, 2);

    var promo = CreateBuyXGetY(buyQty: 2, getQty: 1);
    var result = PromotionCalculator.Calculate(promo, order.Items);

    // 4 items / 2 buyQty = 2 groups → 2 free (2x cheapest = 2x35k)
    result.TotalDiscount.Should().Be(70_000);
  }

  [Fact]
  public void Buy2Get1_OrderScope_With1Item_ShouldReturnZero()
  {
    var order = CreateOrder();
    order.AddItem(1, "Cà phê", 35_000, 1);

    var promo = CreateBuyXGetY(buyQty: 2, getQty: 1);
    var result = PromotionCalculator.Calculate(promo, order.Items);

    result.TotalDiscount.Should().Be(0);
  }

  // ── Buy 2 Get 1 (Product scope) ───────────────────────────────────

  [Fact]
  public void Buy2Get1_ProductScope_With2MatchingItems_ShouldGiveCheapestFree()
  {
    var order = CreateOrder();
    order.AddItem(1, "Latte", 40_000, 2); // matching
    order.AddItem(2, "Bánh", 25_000, 3);  // not in scope

    var promo = CreateBuyXGetY(
      buyQty: 2, getQty: 1,
      scope: PromotionScope.Product,
      applicableProductIds: [1]);

    var result = PromotionCalculator.Calculate(promo, order.Items);

    result.TotalDiscount.Should().Be(40_000); // 1 free Latte
  }

  [Fact]
  public void Buy2Get1_ProductScope_NoMatchingItems_ShouldReturnZero()
  {
    var order = CreateOrder();
    order.AddItem(1, "Bánh", 25_000, 5); // product 1, not in scope

    var promo = CreateBuyXGetY(
      buyQty: 2, getQty: 1,
      scope: PromotionScope.Product,
      applicableProductIds: [99]); // product 99 not in order

    var result = PromotionCalculator.Calculate(promo, order.Items);

    result.TotalDiscount.Should().Be(0);
  }

  // ── Buy 2 Get 1 — cross-product (GetFromProductIds, items NOT in order) ──

  [Fact]
  public void Buy2Get1_CrossProduct_ItemsNotInOrder_ShouldUseCheapestExternalProduct()
  {
    // Tình huống: mua 2 Cà Phê, tặng 1 Hạt Dưa/Hướng Dương (chưa có trong order)
    var order = CreateOrder();
    order.AddItem(1, "Cà Phê Nâu", 25_000, 1);
    order.AddItem(2, "Cà Phê Đen", 25_000, 1);

    var promo = CreateBuyXGetY(
      buyQty: 2, getQty: 1,
      scope: PromotionScope.Category,  // Category scope, nhưng test dùng Order scope cho đơn giản
      getFromProductIds: [10, 11]);

    // External free products (fetched from DB by handler)
    var externalFreeProducts = new List<Product>
    {
      Product.Create(categoryId: 2, name: "Hạt Dưa",      price: 15_000),
      Product.Create(categoryId: 2, name: "Hướng Dương",  price: 20_000),
    };

    // Override scope to Order for this test (scopedItems = all items = 2 coffees)
    var orderScopePromo = Promotion.Create(
      name: "Mua 2 Cà Phê tặng 1 Hạt",
      code: "TEST2",
      codeVisibility: CodeVisibility.Public,
      description: null,
      discountType: DiscountType.BuyXGetY,
      discountValue: 0,
      maxDiscountAmount: null,
      buyQuantity: 2,
      getQuantity: 1,
      scope: PromotionScope.Order,
      applicableProductIds: null,
      applicableCategoryIds: null,
      getFromProductIds: [10, 11],
      getFromCategoryIds: null,
      minOrderAmount: null,
      startDate: ValidStart,
      endDate: null,
      maxUsage: null);

    var result = PromotionCalculator.Calculate(orderScopePromo, order.Items,
      productCategoryMap: null, externalFreeProducts: externalFreeProducts);

    result.TotalDiscount.Should().Be(15_000); // cheapest = Hạt Dưa
    result.FreeGifts.Should().HaveCount(1);
    result.FreeGifts![0].ProductId.Should().Be(externalFreeProducts[0].Id); // Hạt Dưa
    result.FreeGifts![0].Quantity.Should().Be(1);
  }

  [Fact]
  public void Buy2Get1_CrossProduct_2Groups_ShouldGive2FreeGifts()
  {
    var order = CreateOrder();
    order.AddItem(1, "Cà Phê", 25_000, 4); // 4 / 2 = 2 groups

    var externalFreeProducts = new List<Product>
    {
      Product.Create(categoryId: 2, name: "Hạt Dưa",     price: 15_000),
      Product.Create(categoryId: 2, name: "Hướng Dương", price: 20_000),
    };

    var promo = Promotion.Create(
      name: "Test",
      code: "TEST3",
      codeVisibility: CodeVisibility.Public,
      description: null,
      discountType: DiscountType.BuyXGetY,
      discountValue: 0,
      maxDiscountAmount: null,
      buyQuantity: 2,
      getQuantity: 1,
      scope: PromotionScope.Order,
      applicableProductIds: null,
      applicableCategoryIds: null,
      getFromProductIds: [10, 11],
      getFromCategoryIds: null,
      minOrderAmount: null,
      startDate: ValidStart,
      endDate: null,
      maxUsage: null);

    var result = PromotionCalculator.Calculate(promo, order.Items,
      productCategoryMap: null, externalFreeProducts: externalFreeProducts);

    // 2 groups → 2 free units → Hạt Dưa (15k) + Hướng Dương (20k)
    result.TotalDiscount.Should().Be(35_000);
    result.FreeGifts.Should().HaveCount(2);
  }
}
