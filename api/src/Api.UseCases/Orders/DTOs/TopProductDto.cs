namespace Api.UseCases.Orders.DTOs;

/// <summary>Product ranked by total quantity sold (non-gift items).</summary>
public record TopProductDto(string Name, int Qty, int Pct, decimal Revenue);
