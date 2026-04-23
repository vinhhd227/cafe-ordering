using System.Text;
using Api.Core.Aggregates.PrinterAggregate;
using Api.Infrastructure.Printing.Abstractions;

namespace Api.Infrastructure.Printing.Formatters;

/// <summary>
/// TSPL (TSC Printer Language) formatter for label printers such as the ITP3300.
/// Generates ASCII text commands at 203 DPI (8 dots/mm).
/// TSPL requires CRLF (\r\n) line endings — AppendLine is intentionally avoided.
/// </summary>
public class TsplPrintFormatter : IPrintFormatter
{
  // 203 DPI = 8 dots per mm
  private const int DotsPerMm = 8;
  private const int MarginX   = 0;   // no software margin — let hardware offset determine edge distance

  // Built-in TSPL font dimensions at xmul=1,ymul=1 (width x height in dots)
  // Font "1": 8x12, Font "2": 12x20, Font "3": 24x24
  private const int Font1W = 8,  Font1H = 12;
  private const int Font2W = 12, Font2H = 20;

  public bool Supports(PrintFormatterType type) => type == PrintFormatterType.Tspl;

  public byte[] FormatDrinkLabel(DrinkLabelData data, PrinterConfig config)
  {
    int widthMm   = config.PaperWidthMm;
    int widthDots = widthMm * DotsPerMm;
    int usable    = widthDots - MarginX * 2;

    var sb = new StringBuilder();
    AppendHeader(sb, widthMm, heightMm: 40);

    int y = 8;

    // Row 1: Table code (left) + Order number (right) — Font "2"
    string tableStr = data.TableCode != null ? $"Ban: {data.TableCode}" : "Mang di";
    Line(sb, $"TEXT {MarginX},{y},\"2\",0,1,1,\"{Esc(tableStr)}\"");
    int orderX = widthDots - MarginX - data.OrderNumber.Length * Font2W;
    if (orderX > MarginX)
      Line(sb, $"TEXT {orderX},{y},\"2\",0,1,1,\"{Esc(data.OrderNumber)}\"");
    y += Font2H + 2;

    // Separator bar
    Line(sb, $"BAR {MarginX},{y},{usable},2");
    y += 8;

    // Product name — Font "2" doubled (24x40 per char), centered
    string product  = StripDiacritics(Truncate(data.ProductName, usable / (Font2W * 2)));
    int    prodDots = product.Length * Font2W * 2;
    int    prodX    = Math.Max(MarginX, (widthDots - prodDots) / 2);
    Line(sb, $"TEXT {prodX},{y},\"2\",0,2,2,\"{Esc(product)}\"");
    y += Font2H * 2 + 6;

    // Options line — Font "1"
    var opts = new List<string>();
    if (data.Temperature != null) opts.Add(FormatTemperature(data.Temperature));
    if (data.IceLevel    != null) opts.Add($"Da: {FormatLevel(data.IceLevel)}");
    if (data.SugarLevel  != null) opts.Add($"Duong: {FormatLevel(data.SugarLevel)}");
    if (opts.Count > 0)
    {
      Line(sb, $"TEXT {MarginX},{y},\"1\",0,1,1,\"{Esc(string.Join(" | ", opts))}\"");
      y += Font1H + 4;
    }

    // Note
    if (!string.IsNullOrWhiteSpace(data.Note))
    {
      string note = StripDiacritics(Truncate(data.Note, usable / Font1W));
      Line(sb, $"TEXT {MarginX},{y},\"1\",0,1,1,\"{Esc($"Ghi chu: {note}")}\"");
      y += Font1H + 4;
    }

    // Quantity > 1
    if (data.Quantity > 1)
    {
      Line(sb, $"TEXT {MarginX},{y},\"1\",0,1,1,\"So luong: x{data.Quantity}\"");
      y += Font1H + 4;
    }

    // Takeaway
    if (data.IsTakeaway)
    {
      Line(sb, $"TEXT {MarginX},{y},\"2\",0,1,1,\"** MANG DI **\"");
      y += Font2H + 4;
    }

    // Bottom separator bar
    Line(sb, $"BAR {MarginX},{y},{usable},2");
    y += 8;

    // Footer: time (left) + item index (right) — Font "1"
    string time  = data.PrintedAt.ToString("HH:mm dd/MM");
    string index = $"{data.ItemIndex}/{data.TotalItems}";
    Line(sb, $"TEXT {MarginX},{y},\"1\",0,1,1,\"{time}\"");
    int indexX = widthDots - MarginX - index.Length * Font1W;
    if (indexX > MarginX)
      Line(sb, $"TEXT {indexX},{y},\"1\",0,1,1,\"{index}\"");

    Line(sb, "PRINT 1");

    return Encoding.ASCII.GetBytes(sb.ToString());
  }

  public byte[] FormatTestPage(PrinterConfig config)
  {
    int widthMm = config.PaperWidthMm;

    var sb = new StringBuilder();
    AppendHeader(sb, widthMm, heightMm: 40);

    Line(sb, $"TEXT {MarginX},8,\"3\",0,1,1,\"HELLO\"");
    Line(sb, $"TEXT {MarginX},36,\"2\",0,1,1,\"TEST PRINT\"");
    Line(sb, $"TEXT {MarginX},62,\"1\",0,1,1,\"Printer: {Esc(config.Name)}\"");
    Line(sb, $"TEXT {MarginX},78,\"1\",0,1,1,\"Paper:   {widthMm}mm\"");
    Line(sb, $"TEXT {MarginX},94,\"1\",0,1,1,\"Time: {DateTime.Now:HH:mm:ss}\"");
    Line(sb, "PRINT 1");

    return Encoding.ASCII.GetBytes(sb.ToString());
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  // TSPL requires CRLF line endings
  private static void Line(StringBuilder sb, string command) =>
    sb.Append(command).Append("\r\n");

  private static void AppendHeader(StringBuilder sb, int widthMm, int heightMm)
  {
    Line(sb, $"SIZE {widthMm} mm,{heightMm} mm");
    Line(sb, "GAP 3 mm,0");
    Line(sb, "CLS");
  }

  // Escape double-quotes inside TSPL text strings
  private static string Esc(string s) => s.Replace("\"", "\\\"");

  private static string Truncate(string s, int max) =>
    max <= 0 ? s : s.Length > max ? s[..max] : s;

  // Strip Vietnamese diacritics — TSPL printers only support ASCII
  private static string StripDiacritics(string s)
  {
    var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
    var sb         = new System.Text.StringBuilder();
    foreach (var c in normalized)
    {
      var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
      if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
        sb.Append(c);
    }
    return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D');
  }

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
}
