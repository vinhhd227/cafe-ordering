using Api.Core.Aggregates.CustomerAggregate;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Register;

/// <summary>
/// Handler for user registration.
/// Creates Customer aggregate first, then links ApplicationUser to it.
/// </summary>
public class RegisterHandler : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
{
  private readonly IRepository<Customer> _customerRepo;
  private readonly IIdentityService _identityService;

  public RegisterHandler(
    IRepository<Customer> customerRepo,
    IIdentityService identityService)
  {
    _customerRepo = customerRepo;
    _identityService = identityService;
  }

  public async ValueTask<Result<RegisterResponse>> Handle(RegisterCommand cmd, CancellationToken ct)
  {
    // 1. Generate external Customer ID (placeholder - replace with actual external system integration)
    var externalCustomerId = Guid.NewGuid().ToString();

    // 2. Create Customer aggregate (domain, single source of truth)
    var customer = Customer.Create(externalCustomerId, cmd.FirstName, cmd.LastName, cmd.Email);
    await _customerRepo.AddAsync(customer, ct);

    // 3. Create Identity user and link to Customer
    var identityResult = await _identityService.CreateUserAsync(
      email: cmd.Email,
      password: cmd.Password,
      customerId: customer.Id);  // customer.Id is string

    if (!identityResult.IsSuccess)
    {
      // Rollback customer creation if identity fails
      await _customerRepo.DeleteAsync(customer, ct);
      var errorMessage = identityResult.Errors.Any()
        ? string.Join(", ", identityResult.Errors)
        : "User creation failed";
      return Result<RegisterResponse>.Error(errorMessage);
    }

    return Result<RegisterResponse>.Success(
      new RegisterResponse(customer.Id, customer.Email));
  }
}
