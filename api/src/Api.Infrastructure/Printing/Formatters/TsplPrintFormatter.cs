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
    int paperWmm = config.PaperWidthMm; // 55
    const int paperHmm = 33;

    // Label box: 50×30mm centered on 58×34mm paper
    const int boxWmm = 50, boxHmm = 30;
    int boxW  = boxWmm * DotsPerMm;  // 400
    int boxH  = boxHmm * DotsPerMm;  // 240
    int boxX1 = (paperWmm * DotsPerMm - boxW) / 2;  // 32
    int boxY1 = (paperHmm * DotsPerMm - boxH) / 2;  // 16
    int boxX2 = boxX1 + boxW - 1;   // 431
    int boxY2 = boxY1 + boxH - 1;   // 255

    const int pad = 32;  // inner padding from box edge
    int cx  = boxX1 + pad;   // 40
    int cxR = boxX2 - pad;   // 423
    int cW  = cxR - cx;      // 383

    var sb = new StringBuilder();
    AppendHeader(sb, paperWmm, paperHmm);

    // Rounded border box (radius 8 dots ≈ 1mm)
    Line(sb, $"BOX {boxX1},{boxY1},{boxX2},{boxY2},2,8");

    int y = boxY1 + pad;  // 24

    // Row 1: table code (left) + order number (right) — Font "2"
    string tableStr = data.TableCode != null ? $"Ban: {data.TableCode}" : "Mang di";
    Line(sb, $"TEXT {cx},{y},\"2\",0,1,1,\"{Esc(tableStr)}\"");
    int orderX = cxR - data.OrderNumber.Length * Font2W;
    if (orderX > cx)
      Line(sb, $"TEXT {orderX},{y},\"2\",0,1,1,\"{Esc(data.OrderNumber)}\"");
    y += Font2H + 4;

    // Separator
    Line(sb, $"BAR {cx},{y},{cW},2");
    y += 2 + 6;

    // Product name — Font "2" ×2, centered, word-wrapped
    int nameMaxChars = cW / (Font2W * 2);  // 383/24 ≈ 15 chars per line
    var nameLines    = WrapWords(StripDiacritics(data.ProductName), nameMaxChars);
    foreach (var nameLine in nameLines)
    {
      int lineW = nameLine.Length * Font2W * 2;
      int nameX = cx + Math.Max(0, (cW - lineW) / 2);
      Line(sb, $"TEXT {nameX},{y},\"2\",0,2,2,\"{Esc(nameLine)}\"");
      y += Font2H * 2 + 4;
    }
    y += 2;

    // Options — Font "1"
    var opts = new List<string>();
    if (data.Temperature != null) opts.Add(FormatTemperature(data.Temperature));
    if (data.IceLevel    != null) opts.Add($"Da: {FormatLevel(data.IceLevel)}");
    if (data.SugarLevel  != null) opts.Add($"Duong: {FormatLevel(data.SugarLevel)}");
    if (opts.Count > 0)
    {
      Line(sb, $"TEXT {cx},{y},\"1\",0,1,1,\"{Esc(string.Join(" | ", opts))}\"");
      y += Font1H + 4;
    }

    // Note — Font "1"
    if (!string.IsNullOrWhiteSpace(data.Note))
    {
      string note = StripDiacritics(Truncate(data.Note, cW / Font1W));
      Line(sb, $"TEXT {cx},{y},\"1\",0,1,1,\"{Esc($"Ghi chu: {note}")}\"");
    }

    // Price — right-aligned, Font "2", anchored above bottom separator
    int yPrice  = boxY2 - pad - Font1H - 6 - 2 - 6 - Font2H;  // 235-6-2-6-20=201
    string priceStr = FormatVnd(data.UnitPrice);
    int priceX = cxR - priceStr.Length * Font2W;
    if (priceX >= cx)
      Line(sb, $"TEXT {priceX},{yPrice},\"2\",0,1,1,\"{priceStr}\"");

    // Footer anchored to bottom of box
    int yFooter = boxY2 - pad - Font1H;   // 235
    int ySepBot = yFooter - 6;            // 229
    Line(sb, $"BAR {cx},{ySepBot},{cW},2");

    string time  = data.PrintedAt.ToString("HH:mm dd/MM");
    string index = $"{data.ItemIndex}/{data.TotalItems}";
    Line(sb, $"TEXT {cx},{yFooter},\"1\",0,1,1,\"{time}\"");
    int indexX = cxR - index.Length * Font1W;
    if (indexX > cx)
      Line(sb, $"TEXT {indexX},{yFooter},\"1\",0,1,1,\"{index}\"");

    Line(sb, "PRINT 1");
    return Encoding.ASCII.GetBytes(sb.ToString());
  }

  private static List<string> WrapWords(string s, int max)
  {
    var result = new List<string>();
    if (max <= 0) { result.Add(s); return result; }
    var words = s.Split(' ');
    var line  = new StringBuilder();
    foreach (var word in words)
    {
      string w = word.Length > max ? word[..max] : word;
      if (line.Length == 0)
      {
        line.Append(w);
      }
      else if (line.Length + 1 + w.Length <= max)
      {
        line.Append(' ').Append(w);
      }
      else
      {
        result.Add(line.ToString());
        line.Clear().Append(w);
      }
    }
    if (line.Length > 0) result.Add(line.ToString());
    return result;
  }

  // TSPL is for label printers — receipts are not supported
  public byte[] FormatReceipt(ReceiptData data, PrinterConfig config) =>
    throw new NotSupportedException("Receipt printing is not supported on TSPL label printers.");

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
}
