namespace Api.UseCases.Orders.DTOs;

public record HourlyRevenueDto(int Hour, decimal Revenue, int OrderCount);
