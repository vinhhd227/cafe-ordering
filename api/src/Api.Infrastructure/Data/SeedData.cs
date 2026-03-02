using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.ProductAggregate;
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
    if (!context.Categories.Any())
    {
      var traHoaQua     = Category.Create("Trà Hoa Quả");
      var caPhe         = Category.Create("Cà Phê");
      var traMatOng     = Category.Create("Trà Mật Ong - Thảo Mộc");
      var traThai       = Category.Create("Trà Thái");
      var doUongKhac    = Category.Create("Đồ Uống Khác");
      var doAnKem       = Category.Create("Đồ Ăn Kèm");

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
          Product.Create(traHoaQua.Id, "Trà Nhiệt Đới",          29000, "Trà trái cây tổng hợp",        hasIceLevelOption: true, hasSugarLevelOption: true),
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
}
