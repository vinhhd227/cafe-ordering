namespace Api.UseCases.Orders.DTOs;

/// <summary>Category ranked by share of total revenue.</summary>
public record TopCategoryDto(string Name, int Pct, decimal Revenue);
