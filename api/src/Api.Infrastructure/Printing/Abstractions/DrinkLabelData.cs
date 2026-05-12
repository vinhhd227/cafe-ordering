namespace Api.Infrastructure.Printing.Abstractions;

public record DrinkLabelData(
  string                 OrderNumber,
  string?                TableCode,
  string                 ProductName,
  int                    Quantity,
  decimal                UnitPrice,
  IReadOnlyList<string>  Options,
  string?                Note,
  bool                   IsTakeaway,
  int                    ItemIndex,
  int                    TotalItems,
  DateTime               PrintedAt
);
