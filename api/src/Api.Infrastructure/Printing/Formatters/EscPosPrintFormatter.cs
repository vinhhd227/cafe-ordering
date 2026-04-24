using Api.Core.Aggregates.PrinterAggregate;
using Api.Infrastructure.Printing.Abstractions;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using QRCoder;

namespace Api.Infrastructure.Printing.Formatters;

public class EscPosPrintFormatter : IPrintFormatter
{
  private static readonly EPSON E = new();

  public bool Supports(PrintFormatterType type) => type == PrintFormatterType.EscPos;

  public byte[] FormatDrinkLabel(DrinkLabelData data, PrinterConfig config)
  {
    int charWidth = config.PaperWidthMm <= 60 ? 32 : 48;
    var sep = new string('-', charWidth);

    var parts = new List<byte[]>
    {
      E.Initialize(),

      // Header: table + order number
      E.LeftAlign(),
    };

    string left  = data.TableCode != null ? $"Ban: {data.TableCode}" : "Mang di";
    string right = data.OrderNumber;
    parts.Add(E.PrintLine(PadBetween(left, right, charWidth)));
    parts.Add(E.PrintLine(sep));

    // Product name — bold + double size
    parts.Add(E.CenterAlign());
    parts.Add(E.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight | PrintStyle.DoubleWidth));
    parts.Add(E.PrintLine(Truncate(StripDiacritics(data.ProductName), charWidth / 2)));
    parts.Add(E.SetStyles(PrintStyle.None));

    // Options
    parts.Add(E.LeftAlign());
    var opts = new List<string>();
    if (data.Temperature != null) opts.Add(FormatTemperature(data.Temperature));
    if (data.IceLevel    != null) opts.Add($"Da: {FormatLevel(data.IceLevel)}");
    if (data.SugarLevel  != null) opts.Add($"Duong: {FormatLevel(data.SugarLevel)}");
    if (opts.Count > 0) parts.Add(E.PrintLine(string.Join(" | ", opts)));

    // Note
    if (!string.IsNullOrWhiteSpace(data.Note))
      parts.Add(E.PrintLine($"Ghi chu: {StripDiacritics(data.Note)}"));

    // Quantity > 1
    if (data.Quantity > 1)
      parts.Add(E.PrintLine($"So luong: x{data.Quantity}"));

    // Takeaway
    if (data.IsTakeaway)
    {
      parts.Add(E.SetStyles(PrintStyle.Bold));
      parts.Add(E.PrintLine("** MANG DI **"));
      parts.Add(E.SetStyles(PrintStyle.None));
    }

    parts.Add(E.PrintLine(sep));

    // Footer: time + index
    string time  = data.PrintedAt.ToString("HH:mm dd/MM");
    string index = $"{data.ItemIndex}/{data.TotalItems}";
    parts.Add(E.PrintLine(PadBetween(time, index, charWidth)));

    parts.Add(Feed(5));
    parts.Add(Cut);

    return ByteSplicer.Combine(parts.ToArray());
  }

  public byte[] FormatReceipt(ReceiptData data, PrinterConfig config)
  {
    int charWidth = config.PaperWidthMm <= 60 ? 32 : 48;
    var sep  = new string('-', charWidth);
    var sep2 = new string('=', charWidth);

    var parts = new List<byte[]> { E.Initialize() };

    // Header
    parts.Add(E.CenterAlign());
    if (!string.IsNullOrWhiteSpace(data.CafeName))
    {
      parts.Add(E.SetStyles(PrintStyle.Bold));
      parts.Add(E.PrintLine(Truncate(StripDiacritics(data.CafeName), charWidth)));
      parts.Add(E.SetStyles(PrintStyle.None));
    }
    if (!string.IsNullOrWhiteSpace(data.CafeAddress))
      parts.Add(E.PrintLine(Truncate(StripDiacritics(data.CafeAddress), charWidth)));
    if (!string.IsNullOrWhiteSpace(data.CafePhone))
      parts.Add(E.PrintLine($"Tel: {data.CafePhone}"));
    parts.Add(E.PrintLine(sep2));

    // Order + table
    parts.Add(E.LeftAlign());
    string orderLine = $"Order: {data.OrderNumber}";
    string tablePart = data.TableCode != null ? $"Ban: {data.TableCode}" : "Mang di";
    parts.Add(E.PrintLine(PadBetween(orderLine, tablePart, charWidth)));
    parts.Add(E.PrintLine($"Thoi gian: {data.PrintedAt:HH:mm dd/MM/yyyy}"));
    parts.Add(E.PrintLine(sep));

    // Items
    foreach (var item in data.Items)
    {
      string name   = StripDiacritics(item.ProductName);
      string qty    = $"x{item.Quantity}";
      string price  = FormatVnd(item.TotalPrice);

      // Name line with qty on left, price on right
      int remaining = charWidth - qty.Length - 1 - price.Length;
      string nameTrunc = Truncate(name, Math.Max(0, remaining));
      string namePadded = nameTrunc.PadRight(remaining);
      parts.Add(E.PrintLine($"{qty} {namePadded}{price}"));

      // Unit price if qty > 1
      if (item.Quantity > 1)
      {
        string unitLine = $"  ({FormatVnd(item.UnitPrice)} x {item.Quantity})";
        parts.Add(E.PrintLine(unitLine));
      }
    }
    parts.Add(E.PrintLine(sep));

    // Subtotal / discount / total
    if (data.Discount > 0)
    {
      parts.Add(E.PrintLine(PadBetween("Tong:", FormatVnd(data.Subtotal), charWidth)));
      parts.Add(E.PrintLine(PadBetween("Giam gia:", $"-{FormatVnd(data.Discount)}", charWidth)));
    }

    parts.Add(E.SetStyles(PrintStyle.Bold));
    parts.Add(E.PrintLine(PadBetween("THANH TOAN:", FormatVnd(data.Total), charWidth)));
    parts.Add(E.SetStyles(PrintStyle.None));

    // Payment method
    if (!string.IsNullOrWhiteSpace(data.PaymentMethod))
    {
      string method = data.PaymentMethod switch
      {
        "CASH"          => "Tien mat",
        "BANK_TRANSFER" => "Chuyen khoan",
        _               => data.PaymentMethod
      };
      parts.Add(E.PrintLine(PadBetween("Phuong thuc:", method, charWidth)));
    }

    parts.Add(E.PrintLine(sep2));

    // QR code (optional)
    if (!string.IsNullOrWhiteSpace(data.QrUrl))
    {
      parts.Add(E.CenterAlign());
      parts.Add(RenderQr(data.QrUrl, moduleSize: 4));
      parts.Add(E.PrintLine(""));
    }

    // Footer
    parts.Add(E.CenterAlign());
    parts.Add(E.PrintLine("Cam on quy khach!"));
    parts.Add(E.LeftAlign());

    parts.Add(Feed(5));
    parts.Add(Cut);

    return ByteSplicer.Combine(parts.ToArray());
  }

  public byte[] FormatTestPage(PrinterConfig config)
  {
    return ByteSplicer.Combine(
      E.Initialize(),
      E.CenterAlign(),
      E.SetStyles(PrintStyle.Bold),
      E.PrintLine("HELLO"),
      E.SetStyles(PrintStyle.None),
      E.PrintLine("TEST PRINT"),
      E.PrintLine("----------"),
      E.LeftAlign(),
      E.PrintLine($"Printer: {config.Name}"),
      E.PrintLine($"Paper:   {config.PaperWidthMm}mm"),
      E.PrintLine("----------"),
      E.PrintLine($"Time: {DateTime.Now:HH:mm:ss}"),
      Feed(5),
      Cut
    );
  }

  /// Render QR code as ESC/POS raster bitmap (GS v 0).
  /// moduleSize: pixels per QR module (3–5 looks good on 80mm).
  private static byte[] RenderQr(string content, int moduleSize = 4)
  {
    using var qrGenerator = new QRCodeGenerator();
    var qrData  = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
    var matrix  = qrData.ModuleMatrix; // List<BitArray>, each = one row
    int modules = matrix.Count;
    int px      = modules * moduleSize; // total pixels wide & tall

    int bytesPerRow = (px + 7) / 8;
    var pixels      = new byte[bytesPerRow * px];

    for (int row = 0; row < modules; row++)
    {
      for (int mr = 0; mr < moduleSize; mr++) // repeat each module row
      {
        int y = row * moduleSize + mr;
        for (int col = 0; col < modules; col++)
        {
          if (!matrix[row][col]) continue; // white → skip (0 = white for ESC/POS)
          for (int mc = 0; mc < moduleSize; mc++) // repeat each module col
          {
            int x    = col * moduleSize + mc;
            int bit  = 7 - (x % 8);
            pixels[y * bytesPerRow + x / 8] |= (byte)(1 << bit);
          }
        }
      }
    }

    // GS v 0: 1D 76 30 mode xL xH yL yH [data]
    var cmd = new byte[8 + pixels.Length];
    cmd[0] = 0x1D; cmd[1] = 0x76; cmd[2] = 0x30; cmd[3] = 0x00; // GS v 0, normal
    cmd[4] = (byte)(bytesPerRow & 0xFF);
    cmd[5] = (byte)(bytesPerRow >> 8);
    cmd[6] = (byte)(px & 0xFF);
    cmd[7] = (byte)(px >> 8);
    Buffer.BlockCopy(pixels, 0, cmd, 8, pixels.Length);
    return cmd;
  }

  // Raw feed + cut — compatible with Chinese printers that don't support GS V
  private static byte[] Feed(int lines) => System.Text.Encoding.ASCII.GetBytes(new string('\n', lines));
  private static readonly byte[] Cut = [0x1B, 0x69]; // ESC i

  private static string FormatVnd(decimal value) =>
    ((long)value).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + "d";

  private static string FormatTemperature(string v) => v switch
  {
    "HOT"  => "Nong",
    "COLD" => "Lanh",
    _      => v
  };

  private static string FormatLevel(string v) => v switch
  {
    "LESS"   => "It",
    "NORMAL" => "Vua",
    "MORE"   => "Nhieu",
    _        => v
  };

  private static string PadBetween(string left, string right, int width)
  {
    int padding = width - left.Length - right.Length;
    return padding > 0
      ? left + new string(' ', padding) + right
      : $"{left} {right}";
  }

  private static string Truncate(string s, int max) =>
    s.Length > max ? s[..max] : s;

  // Strip Vietnamese diacritics → ASCII-safe for printers without Unicode support
  private static string StripDiacritics(string s)
  {
    var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
    var sb = new System.Text.StringBuilder();
    foreach (var c in normalized)
    {
      var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
      if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
        sb.Append(c);
    }
    // Replace Vietnamese characters that survive normalization
    return sb.ToString()
      .Replace('đ', 'd').Replace('Đ', 'D');
  }
}
