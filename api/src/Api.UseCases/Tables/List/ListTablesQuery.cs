using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.List;

public record ListTablesQuery : IQuery<Result<List<TableDto>>>;
