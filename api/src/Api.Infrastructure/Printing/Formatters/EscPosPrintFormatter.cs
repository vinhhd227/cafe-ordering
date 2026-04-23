using Api.Core.Aggregates.PrinterAggregate;
using Api.Infrastructure.Printing.Abstractions;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;

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

    parts.Add(E.FeedLines(25));
    parts.Add(E.FullCutAfterFeed(3));

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
      E.FeedLines(25),
      E.FullCutAfterFeed(3)
    );
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
