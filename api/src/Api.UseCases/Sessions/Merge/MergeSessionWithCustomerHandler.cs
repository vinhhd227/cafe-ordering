using Api.Core.Aggregates.CustomerAggregate;
using Api.Core.Aggregates.CustomerAggregate.Specifications;
using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;

namespace Api.UseCases.Sessions.Merge;

public class MergeSessionWithCustomerHandler(
  IRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Customer> customerRepository)
  : ICommandHandler<MergeSessionWithCustomerCommand, Result>
{
  public async ValueTask<Result> Handle(MergeSessionWithCustomerCommand request, CancellationToken ct)
  {
    var sessionSpec = new SessionByIdSpec(request.SessionId);
    var session = await sessionRepository.FirstOrDefaultAsync(sessionSpec, ct);

    if (session is null)
      return Result.NotFound($"Session {request.SessionId} not found.");

    if (session.Status == GuestSessionStatus.Closed)
      return Result.Conflict("Cannot merge a closed session.");

    var customerSpec = new CustomerByIdSpec(request.CustomerId);
    var customer = await customerRepository.FirstOrDefaultAsync(customerSpec, ct);

    if (customer is null)
      return Result.NotFound($"Customer {request.CustomerId} not found.");

    session.MergeWithCustomer(request.CustomerId);
    await sessionRepository.UpdateAsync(session, ct);

    return Result.Success();
  }
}
