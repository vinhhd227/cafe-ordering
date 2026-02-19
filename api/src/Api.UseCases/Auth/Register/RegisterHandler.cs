using Api.Core.Aggregates.CustomerAggregate;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Register;

/// <summary>
/// Handler for user registration.
/// Flow: Create Identity user first → get IdentityGuid → create Customer → link via IdentityGuid.
/// Two separate DBs, no FK constraint between them.
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
    // 1. Create Identity user first — get the identity user ID
    var identityResult = await _identityService.CreateUserAsync(
      email: cmd.Email,
      password: cmd.Password);

    if (!identityResult.IsSuccess)
    {
      var errorMessage = identityResult.Errors.Any()
        ? string.Join(", ", identityResult.Errors)
        : "User creation failed";
      return Result<RegisterResponse>.Error(errorMessage);
    }

    var identityGuid = identityResult.Value; // ApplicationUser.Id.ToString()

    // 2. Create Customer aggregate with IdentityGuid link (no FK — string value only)
    var customer = Customer.Create(cmd.FirstName, cmd.LastName, cmd.Email);
    customer.LinkToIdentity(identityGuid);
    await _customerRepo.AddAsync(customer, ct);

    return Result<RegisterResponse>.Success(
      new RegisterResponse(customer.Id, customer.Email));
  }
}
