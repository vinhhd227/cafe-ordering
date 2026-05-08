using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.NotificationAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.TableAggregate;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Data;

/// <summary>
/// Seeds business data into AppDbContext.
/// Identity seeding (users/roles) is handled by IdentitySeedData in the Identity folder.
/// </summary>
public static class SeedData
{
  public static async Task InitializeAsync(AppDbContext context, ILogger? logger = null)
  {
    await SeedRecipesAsync(context, logger);
    if (!context.Categories.Any())
    {
      var traHoaQua     = Category.Create("Trà Hoa Quả",  null, null, 1);
      var caPhe         = Category.Create("Cà Phê",        null, null, 2);
      var traMatOng     = Category.Create("Trà Mật Ong - Thảo Mộc", null, null, 3);
      var traThai       = Category.Create("Trà Thái",      null, null, 4);
      var doUongKhac    = Category.Create("Đồ Uống Khác",  null, null, 5);
      var doAnKem       = Category.Create("Đồ Ăn Kèm",    null, null, 6);

      var categories = new[] { traHoaQua, caPhe, traMatOng, traThai, doUongKhac, doAnKem };
      context.Categories.AddRange(categories);
      await context.SaveChangesAsync();

      logger?.LogInformation("Seeded {Count} categories", categories.Length);

      if (!context.Products.Any())
      {
        var products = new[]
        {
          // Trà Hoa Quả
          Product.Create(traHoaQua.Id, "Trà Chanh",              25000, "Trà chanh tươi mát",            hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Tắc",                25000, "Trà tắc thanh mát",             hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Dứa Chanh Dây",      29000, "Trà dứa chanh dây nhiệt đới",  hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Hoa Quả Nhiệt Đới",  29000, "Trà hoa quả nhiệt đới tổng hợp", hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Xoài Chanh Dây",     29000, "Trà trái cây nhiệt đới",       hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Mãng Cầu",           35000, "Trà mãng cầu chua ngọt",       hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traHoaQua.Id, "Trà Đào Cam Sả",         35000, "Trà đào cam sả thanh mát",     hasIceLevelOption: true, hasSugarLevelOption: true),

          // Cà Phê
          Product.Create(caPhe.Id, "Cà Phê Đen",   25000, "Cà phê truyền thống",  hasTemperatureOption: true, hasIceLevelOption: true),
          Product.Create(caPhe.Id, "Cà Phê Nâu",   25000, "Cà phê sữa",           hasTemperatureOption: true, hasIceLevelOption: true),
          Product.Create(caPhe.Id, "Bạc Xỉu",      35000, "Sữa nhiều cà phê ít",  hasTemperatureOption: true, hasIceLevelOption: true),
          Product.Create(caPhe.Id, "Cà Phê Muối",  35000, "Cà phê đậm vị phủ kem muối béo nhẹ",  hasIceLevelOption: true),
          Product.Create(caPhe.Id, "Cà Phê Trứng", 40000, "Cà phê truyền thống phủ kem trứng béo mịn"),

          // Trà Mật Ong - Thảo Mộc
          Product.Create(traMatOng.Id, "Trà Gừng Mật Ong",           29000, "Trà gừng ấm nóng"),
          Product.Create(traMatOng.Id, "Trà Hoa Cúc Mật Ong",        29000, "Trà hoa cúc dịu nhẹ"),
          Product.Create(traMatOng.Id, "Trà Đào Cam Quế Mật Ong",    35000, "Trà trái cây thảo mộc"),
          Product.Create(traMatOng.Id, "Trà Cúc Đường Phèn",         35000, "Trà cúc thanh mát"),
          Product.Create(traMatOng.Id, "Trà Mạn",    35000, "Trà xanh nguyên lá pha ấm, hương thơm tự nhiên, vị chát dịu"),

          // Trà Thái
          Product.Create(traThai.Id, "Trà Thái Xanh", 25000, "Trà sữa Thái xanh", hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(traThai.Id, "Trà Thái Đỏ",   25000, "Trà sữa Thái đỏ",   hasIceLevelOption: true, hasSugarLevelOption: true),

          // Đồ Uống Khác
          Product.Create(doUongKhac.Id, "Cacao",              29000, "Cacao nóng lạnh truyền thống", hasTemperatureOption: true, hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(doUongKhac.Id, "Cacao Muối",         29000, "Cacao kem muối",               hasTemperatureOption: true, hasIceLevelOption: true),
          Product.Create(doUongKhac.Id, "Phindi Choco",       35000, "Phindi Choco đặc biệt",        hasIceLevelOption: true, hasSugarLevelOption: true),
          Product.Create(doUongKhac.Id, "Phindi Choco Muối",  35000, "Phindi Choco kem muối",        hasIceLevelOption: true),
          Product.Create(doUongKhac.Id, "Matcha Latte",  35000, "Matcha nguyên chất hòa quyện cùng sữa tươi thơm béo", hasIceLevelOption: true, hasSugarLevelOption: true),
          
          // Đồ Ăn Kèm
          Product.Create(doAnKem.Id, "Hạt Hướng Dương", 15000, "Hạt hướng dương rang"),
          Product.Create(doAnKem.Id, "Hạt Bí",          15000, "Hạt bí rang"),
          Product.Create(doAnKem.Id, "Hạt Dưa",         15000, "Hạt dưa rang"),
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        logger?.LogInformation("Seeded {Count} products", products.Length);
      }
    }

    if (!context.NotificationConfigs.Any())
    {
      var configs = new[]
      {
        NotificationConfig.Create(NotificationType.OrderCreated.Name,       ["Staff", "Admin"]),
        NotificationConfig.Create(NotificationType.OrderCancelled.Name,     ["Staff", "Admin"]),
        NotificationConfig.Create(NotificationType.OrderCompleted.Name,     ["Admin"]),
        NotificationConfig.Create(NotificationType.PaymentReceived.Name,    ["Staff", "Admin"]),
        NotificationConfig.Create(NotificationType.ManualOrderCreated.Name, ["Admin"]),
        NotificationConfig.Create(NotificationType.LowStock.Name,           ["Admin"]),
        NotificationConfig.Create(NotificationType.SystemAlert.Name,        ["Admin"]),
      };
      context.NotificationConfigs.AddRange(configs);
      await context.SaveChangesAsync();

      logger?.LogInformation("Seeded {Count} notification configs", configs.Length);
    }

    if (!context.NotificationSettings.Any())
    {
      context.NotificationSettings.Add(NotificationSettings.Create());
      await context.SaveChangesAsync();
      logger?.LogInformation("Seeded default NotificationSettings (RetentionDays=30)");
    }

    if (!context.Tables.Any())
    {
      var tables = new[]
      {
        // Tầng 1
        Table.Create("F1-01"), Table.Create("F1-02"),
        Table.Create("F1-03"), Table.Create("F1-04"),
        // Tầng hầm
        Table.Create("BM-01"), Table.Create("BM-02"), Table.Create("BM-03"),
        Table.Create("BM-04"), Table.Create("BM-05"), Table.Create("BM-06"),
        Table.Create("BM-07"),
        // Quầy bar
        Table.Create("BAR"),
      };

      context.Tables.AddRange(tables);
      await context.SaveChangesAsync();

      logger?.LogInformation("Seeded {Count} tables", tables.Length);
    }
  }

  // ── Recipes ──────────────────────────────────────────────────────────────────
  private static async Task SeedRecipesAsync(AppDbContext context, ILogger? logger)
  {
    if (context.Recipes.Any()) return;

    var recipes = new List<Recipe>
    {
      // ── BaseIngredient ───────────────────────────────────────────────────
      Recipe.Create(
        "Ủ lục trà (trà nhài AMI)",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"30g lục trà nhài (hoặc trà nhài AMI)
800ml nước sôi + 200ml nước lọc (ấm siêu tốc)
Ủ 15p (KHÔNG ĐẬY NẮP)

=> 100ml cốt lục trà / 1 cốc trà hoa quả (cốt chung không dùng kombucha)
=> 40ml cốt lục trà + 60ml kombucha / 1 cốc kombucha (nếu dùng kom)",
        yield: "900ml ≈ 300 cốc trà hoa quả / 500 cốc kombucha"),

      Recipe.Create(
        "Nước đường",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"650ml nước - đun sôi lăn tăn
1kg đường cho vào nồi, khuấy đều tay theo 1 chiều. KHÔNG đảo chiều để tránh bị lại đường
Giảm dần nhiệt độ, đợi giảm 50% nhiệt độ, tắt bếp, tiếp tục khuấy cho đến khi đường tan",
        yield: "1200ml ≈ 40-50 cốc"),

      Recipe.Create(
        "Đánh kem Machiato",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"300ml kem béo (rich lùn)
2gr muối hồng (2gr muối tinh)
Máy đánh trứng (mức cao nhất): 3-4p (nếu dùng phới lồng sẽ ~2p)
KHÔNG đánh quá đặc. Kem mịn, chảy mượt là đạt",
        yield: "~10 cốc",
        notes: "Đậy nắp bảo quản trong tủ mát ~3 ngày. Nếu kem bị tách nước có thể đánh lại cho mịn."),

      Recipe.Create(
        "Cafe số 5 - Pha sẵn phin to",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"200gr cafe cho vào phin
Lắc nhẹ dàn đều cafe
Đặt nhẹ chặn phin, KHÔNG NÉN

Đợt 1: 250ml nước sôi, ủ 15p (có thể để 2h nhưng 15p là ngon nhất)
Đợt 2: 200ml nước sôi - Đợi hết nước - 15p
Đợt 3: 200ml nước sôi - Đợi chảy hết

Thu được ~350ml cốt cà phê",
        yield: "350ml cốt cafe ≈ 7 ly",
        notes: "Đậy nắp bảo quản trong tủ mát 48h - Khuấy đều trước khi dùng\nPhin nhỏ: 1 phin + 28g bột cafe - Dàn đều, chặn phin (KHÔNG NÉN)"),

      Recipe.Create(
        "Ủ coldbrew",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"100g cafe cho vào ca 1L
300ml nước nóng < 90 độ, khuấy đều ủ 5p
700ml nước lọc, đậy nắp
Bỏ tủ lạnh ủ từ 16-24 tiếng

Sau 24 tiếng, lọc cafe sang ca mới, bỏ tủ lạnh bảo quản sử dụng dần"),

      Recipe.Create(
        "Đánh kem muối",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"100ml sữa tươi
150ml kem whipping anchor (để lạnh)
40ml sữa đặc + 50ml kem béo (Muốn đặc hơn: + thêm 50ml kem béo)
20ml đường
4g muối tinh (~4g muối hồng)

Khuấy đều hỗn hợp
Dùng máy đánh trứng đánh bông 3-4p (Mức cao nhất)",
        yield: "~10 cốc - 35gr/cốc",
        notes: "Bảo quản tủ mát 3 ngày - tối đa 1 tuần"),

      Recipe.Create(
        "Đánh kem trứng từ bột kem trứng",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"100ml kem béo
120ml sữa tươi không đường (để lạnh)
10ml siro vani
100g bột kem béo

Khuấy đều, đánh bông, đậy nắp bảo quản lạnh",
        yield: "~15 cốc - 2 thìa định lượng / cốc",
        notes: "Dùng trong 7 ngày."),

      Recipe.Create(
        "Nấu trân châu đường đen",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"Nấu 2,5l nước sôi. Đổ 400g trân châu đen 3A (vừa đổ vừa khuấy 2p)
Đậy nắp, đợi sôi lại
Giảm lửa, đậy nắp đun tiếp 35p (5-10p khuấy 1 lần)
Tắt bếp, ủ thêm 40p
Lọc lấy trân châu rồi để ráo nước

Trộn trân châu với đường theo tỉ lệ:
400g trân châu + 120g siro đường đen
(Mốc: 100g trân châu + 30g siro đường đen)

Cho trân châu lên bếp đun nhỏ lửa, khuấy đều tay 5p
(Nếu dùng bếp từ: giảm lửa ở mức thấp nhất)",
        yield: "400g ≈ 15 cốc",
        notes: "Bảo quản:\n- C1: Cho hỗn hợp trân châu vào khay, để bên ngoài trời sử dụng 6-8 tiếng\n- C2: Cho hỗn hợp trân châu vào nồi ủ ở nhiệt độ 35-45 độ, sử dụng trong ngày"),

      Recipe.Create(
        "Cốt gừng tươi",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"Rửa sạch 50g gừng tươi, không cần cạo vỏ, cho vào máy xay
100ml nước
Xay 35s
Lọc và ép lấy cốt gừng",
        yield: "~10 cốc",
        notes: "Bảo quản tủ mát sử dụng 5 ngày"),

      Recipe.Create(
        "Ủ hồng trà shan tuyết (pha trà sữa)",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"45g hồng trà shan tuyết
960ml nước sôi 100 độ
Ủ 19p (đậy kín nắp)
Lọc trà

Trộn 170g bột kem không sữa Mộc Lan + 250g đá, khuấy tan.",
        yield: "~7 cốc",
        notes: "Đậy kín nắp bảo quản tủ mát sử dụng 3 ngày. Nên để lạnh tối thiểu 1 tiếng trước khi bán hàng."),

      Recipe.Create(
        "Ủ trà ô long",
        RecipeType.BaseIngredient, RecipeCategory.BaseIngredient,
        @"40g trà ô long + 1000ml nước sôi 100 độ
Ủ 15p (đậy kín nắp)
Lọc trà, để nguội",
        yield: "~900ml cốt trà",
        notes: "Bảo quản sử dụng trong 2 ngày"),

      // ── Trà hoa quả ─────────────────────────────────────────────────────
      Recipe.Create(
        "Trà chanh hoàng kim",
        RecipeType.Drink, RecipeCategory.FruitTea,
        @"100ml lục trà + 60ml nước lọc + 20ml Kombucha
(hoặc: 100ml lục trà + 80ml nước lọc)
35-40ml đường + 10ml siro nhiệt đới
15-20ml nước cốt chanh xanh
Thêm đá đến vạch 400ml - lắc đều
2 lát chanh vàng - dầm vào tép lấy nước
Topping thạch trân châu trắng",
        yield: "1 cốc = 240ml cốt tổng"),

      Recipe.Create(
        "Trà mãng cầu",
        RecipeType.Drink, RecipeCategory.FruitTea,
        @"50gr mãng cầu (1 túi đã chia sẵn)
- Cho 2/3 mãng cầu vào bình lắc - Dầm nát
100ml hồng trà + 50ml nước lọc + 35ml đường + 10ml cốt quất
Thêm đá đến vạch 400ml
Khuấy đều cho lạnh hỗn hợp, đổ ra cốc, thêm phần mãng cầu còn lại lên cốc"),

      Recipe.Create(
        "Trà vải hoa hồng",
        RecipeType.Drink, RecipeCategory.FruitTea,
        @"40ml lục trà + 80ml nước lọc + 60ml kombucha
50gr mứt vải hoa hồng
5ml đường
Thêm đá đến vạch 400ml - lắc đều, đổ ra cốc
Trang trí: Hoa hồng khô hoặc vải ngâm (2 quả)
(Nếu hết hoa hồng khô hoặc vải ngâm có thể thêm topping thạch nổ hồng)"),

      Recipe.Create(
        "Trà dưa lưới",
        RecipeType.Drink, RecipeCategory.FruitTea,
        @"100ml lục trà + 80ml nước lọc
50gr mứt dưa lưới
Thêm đá đến vạch 400ml - lắc đều, đổ ra cốc"),

      Recipe.Create(
        "Trà đào kem mặn",
        RecipeType.Drink, RecipeCategory.FruitTea,
        @"2 gói trà cozy đào + 150ml nước nóng (ủ 5p)
80ml hồng trà
30-35ml đường
Thêm đá đến vạch 400ml - lắc đều, đổ ra cốc
40ml kem mặn - đổ lên trên
Trang trí: Thạch đào hoặc đào miếng"),

      // ── Đá xay / Sinh tố ────────────────────────────────────────────────
      Recipe.Create(
        "Sinh tố mãng cầu",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"100g mãng cầu lọc hạt (1 túi mãng cầu đông lạnh = 100g)
30ml kombucha + 40ml nước lọc
20ml đường + 30ml sữa đặc
110g đá
Xay 35s rồi đổ ra cốc",
        yield: "350ml"),

      Recipe.Create(
        "Sinh tố bơ",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"90g bơ (1 túi bơ đông lạnh = 90g)
30ml sữa dừa + 50ml sữa đặc + 40ml nước lọc
100g đá
Xay 35s rồi đổ ra cốc",
        yield: "350ml",
        notes: "Nếu dùng bơ đông lạnh còn cứng nguyên, có thể không bỏ đá mà chỉ đổ 140ml nước lọc."),

      Recipe.Create(
        "Sinh tố cam xoài",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"110g xoài + 15g cam vàng (bỏ hạt, khoảng 2 lát)
40ml kombucha + 15ml sữa đặc
20ml đường
130g đá (2/3 cốc đá 350ml)
Xay 35s rồi đổ ra cốc",
        yield: "350ml"),

      Recipe.Create(
        "Sữa chua đánh đá nguyên vị",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"80g sữa chua + 70ml nước sôi
10ml cốt chanh + 30ml sữa đặc
Đá đến vạch 350ml, lắc mạnh đổ ra cốc
Topping hạt nổ củ năng hồng (2 thìa nhỏ)"),

      Recipe.Create(
        "Sữa chua lắc dưa lưới",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"80g sữa chua + 70ml nước sôi
10ml cốt chanh + 40g mứt dưa lưới
5ml mứt dưa lưới quét lên thành cốc
Đá đến vạch 350ml, lắc mạnh đổ ra cốc
Topping hạt nổ củ năng hồng (2 thìa nhỏ)"),

      Recipe.Create(
        "Sữa chua cà phê",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"80g sữa chua + 70ml nước sôi
30ml sữa đặc + 30ml cà phê
Đá đến vạch 350ml, lắc mạnh đổ ra cốc"),

      Recipe.Create(
        "Matcha latte",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"3g bột matcha + 30ml nước sôi, đánh tan.
100ml sữa tươi không đường + 10ml đường + 15ml sữa đặc, khuấy đều
2/3 cốc đá, đổ sữa ra cốc, đổ matcha lên trên cùng.",
        yield: "400ml"),

      Recipe.Create(
        "Matcha freeze",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"3g bột matcha + 30ml nước sôi, đánh tan.
20ml sữa tươi + 10ml đường + 15ml sữa đặc
20g bột mix + 220g đá
Xay 35s",
        yield: "400ml"),

      Recipe.Create(
        "Chanh tuyết",
        RecipeType.Drink, RecipeCategory.Smoothie,
        @"30ml cốt chanh + 10ml đường + 60ml sữa đặc
1/2 vỏ chanh xanh
270g đá
Xay 35s rồi đổ ra cốc",
        yield: "400ml"),

      // ── Cà phê ──────────────────────────────────────────────────────────
      Recipe.Create(
        "Nâu đá / Nâu nóng",
        RecipeType.Drink, RecipeCategory.Coffee,
        @"Nâu đá:
50ml cà phê + 25ml sữa đặc
Đá đầy cốc

Nâu nóng:
50ml cà phê + 25ml sữa đặc
Dùng cốc gốm + đế đun + nến"),

      Recipe.Create(
        "Bạc xỉu đá",
        RecipeType.Drink, RecipeCategory.Coffee,
        @"60ml sữa tươi không đường + 20ml sữa dừa + 30ml sữa đặc
Đá 2/3 cốc, đổ sữa đã đánh đều vào.
25ml cà phê đổ lên trên sữa
25ml cà phê đánh bọt đổ lên trên cùng"),

      Recipe.Create(
        "Coldbrew kombucha chanh vàng",
        RecipeType.Drink, RecipeCategory.Coffee,
        @"Dầm 3 lát chanh vàng lấy vị, cho vào bình lắc
100ml coldbrew + 50ml kombucha + 30ml đường
Đá đến vạch 300ml, lắc đều rồi đổ ra cốc"),

      Recipe.Create(
        "Cacao đá",
        RecipeType.Drink, RecipeCategory.Coffee,
        @"50ml nước sôi + 10g bột cacao, đánh tan
30ml sữa đặc + 20ml kem béo
Đá đến vạch 300ml",
        yield: "350ml"),

      Recipe.Create(
        "Cacao nóng",
        RecipeType.Drink, RecipeCategory.Coffee,
        @"50ml nước sôi + 10g bột cacao, đánh tan
30ml sữa đặc, 20ml kem béo
120ml nước sôi khuấy đều",
        yield: "300ml"),

      // ── Trà sữa ─────────────────────────────────────────────────────────
      Recipe.Create(
        "Trà sữa truyền thống",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"180ml cốt trà + 35-40ml đường
Đá đến vạch 400ml, lắc đều đổ ra cốc
Topping trân châu"),

      Recipe.Create(
        "Trà sữa trân châu đường đen",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"2 thìa định lượng trân châu đen + 5ml siro đường đen cho vào cốc quét viền 1 vòng
180ml cốt trà + 30ml đường
Đá đến vạch 400ml, lắc đều đổ ra cốc"),

      Recipe.Create(
        "Trà sữa đặc biệt full topping",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"5ml siro đường đen cho vào cốc quét viền 1 vòng + 1 thìa trân châu đen + 1 loại topping khác
180ml cốt trà + 20-25ml đường (chỉnh theo topping, thêm topping thì giảm dần đường)
Đá đến vạch 370ml, KHUẤY ĐỀU
Đổ ra cốc, thêm 1 thìa kem trứng"),

      Recipe.Create(
        "Shan tuyết gừng nóng",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"180ml cốt trà + 5ml đường + 10ml cốt gừng
35ml siro bí đao
100ml nước sôi dùng máy sục nóng"),

      Recipe.Create(
        "Shan tuyết gừng lạnh",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"180ml cốt trà + 10ml đường + 10ml cốt gừng
35ml siro bí đao
Đá đến vạch 400ml, lắc đều đổ ra cốc"),

      Recipe.Create(
        "Ô long sen vàng",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"130ml ô long + 50ml nước lọc
40ml đường + 10 hạt sen
Đá 2/3 cốc
Thêm 2 thìa kem Machiato",
        yield: "500ml"),

      Recipe.Create(
        "Ô long bí đao",
        RecipeType.Drink, RecipeCategory.MilkTea,
        @"25g bột kem không sữa Mộc Lan + 30ml nước sôi, đánh tan
130ml ô long + 35ml siro bí đao
Đá đến vạch 400ml, lắc đều đổ ra cốc",
        yield: "500ml"),
    };

    context.Recipes.AddRange(recipes);
    await context.SaveChangesAsync();
    logger?.LogInformation("Seeded {Count} recipes", recipes.Count);
  }
}
