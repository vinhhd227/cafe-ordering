using Api.UseCases.Sessions.DTOs;

namespace Api.UseCases.Sessions.GetSummary;

public record GetSessionSummaryQuery(Guid SessionId) : IQuery<Result<SessionSummaryDto>>;
