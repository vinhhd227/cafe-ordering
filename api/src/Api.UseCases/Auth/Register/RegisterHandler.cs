using Api.Core.Aggregates.CustomerAggregate;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.Register;

/// <summary>
/// Handler for customer self-registration.
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
    // 1. Create Identity user with chosen username
    var identityResult = await _identityService.CreateUserAsync(
      username: cmd.Username,
      email: cmd.Email,
      password: cmd.Password,
      fullName: cmd.FullName,
      role: "Customer");

    if (!identityResult.IsSuccess)
    {
      var errorMessage = identityResult.Errors.Any()
        ? string.Join(", ", identityResult.Errors)
        : "User creation failed";
      return Result<RegisterResponse>.Error(errorMessage);
    }

    var identityGuid = identityResult.Value; // ApplicationUser.Id.ToString()

    // 2. Split FullName into first/last for Customer aggregate
    var parts = cmd.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var firstName = parts[0];
    var lastName = parts.Length > 1 ? parts[1] : string.Empty;

    // 3. Create Customer aggregate with IdentityGuid link (no FK — string value only)
    var customer = Customer.Create(firstName, lastName, cmd.Email);
    customer.LinkToIdentity(identityGuid);
    await _customerRepo.AddAsync(customer, ct);

    return Result<RegisterResponse>.Success(
      new RegisterResponse(customer.Id, customer.Email));
  }
}
