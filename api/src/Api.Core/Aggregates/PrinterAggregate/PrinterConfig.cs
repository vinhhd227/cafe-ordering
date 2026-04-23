using Ardalis.GuardClauses;
using Ardalis.SharedKernel;

namespace Api.Core.Aggregates.PrinterAggregate;

public class PrinterConfig : SoftDeletableEntity<int>, IAggregateRoot
{
  private PrinterConfig() { }

  public string            Name            { get; private set; } = string.Empty;
  public PrinterRole       Role            { get; private set; } = default!;
  public PrintFormatterType FormatterType  { get; private set; } = default!;
  public PrintTransportType TransportType  { get; private set; } = default!;
  public string            ConnectionParams { get; private set; } = string.Empty;
  public int               PaperWidthMm   { get; private set; }
  public bool              IsDefault      { get; private set; }
  public bool              IsActive       { get; private set; }

  public static PrinterConfig Create(
    string             name,
    PrinterRole        role,
    PrintFormatterType formatterType,
    PrintTransportType transportType,
    string             connectionParams,
    int                paperWidthMm)
  {
    return new PrinterConfig
    {
      Name             = Guard.Against.NullOrWhiteSpace(name),
      Role             = Guard.Against.Null(role),
      FormatterType    = Guard.Against.Null(formatterType),
      TransportType    = Guard.Against.Null(transportType),
      ConnectionParams = Guard.Against.NullOrWhiteSpace(connectionParams),
      PaperWidthMm     = Guard.Against.NegativeOrZero(paperWidthMm),
      IsDefault        = false,
      IsActive         = true
    };
  }

  public void Update(
    string             name,
    PrintFormatterType formatterType,
    PrintTransportType transportType,
    string             connectionParams,
    int                paperWidthMm)
  {
    Name             = Guard.Against.NullOrWhiteSpace(name);
    FormatterType    = Guard.Against.Null(formatterType);
    TransportType    = Guard.Against.Null(transportType);
    ConnectionParams = Guard.Against.NullOrWhiteSpace(connectionParams);
    PaperWidthMm     = Guard.Against.NegativeOrZero(paperWidthMm);
  }

  public void SetAsDefault()  => IsDefault = true;
  public void UnsetDefault()  => IsDefault = false;
  public void Activate()      => IsActive  = true;
  public void Deactivate()    => IsActive  = false;
}
