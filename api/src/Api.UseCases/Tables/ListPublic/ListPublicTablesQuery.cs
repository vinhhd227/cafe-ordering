using Api.UseCases.Tables.DTOs;

namespace Api.UseCases.Tables.ListPublic;

public record ListPublicTablesQuery : IQuery<Result<List<PublicTableDto>>>;
