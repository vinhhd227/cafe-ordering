using Api.UseCases.Auth.Register;
using Mediator;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Request payload for registering a new customer account.
/// </summary>
public sealed class RegisterRequest
{
  /// <summary>Valid email address. Used as the login identifier and must be unique.</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// Password must be at least 8 characters and contain at least one uppercase letter,
  /// one lowercase letter, one digit, and one non-alphanumeric character.
  /// </summary>
  public string Password { get; set; } = string.Empty;

  /// <summary>Customer's first name.</summary>
  public string FirstName { get; set; } = string.Empty;

  /// <summary>Customer's last name.</summary>
  public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Response returned after a successful registration.
/// </summary>
public sealed class RegisterResponse
{
  /// <summary>Unique identifier (GUID) of the newly created customer profile.</summary>
  public string CustomerId { get; init; } = string.Empty;

  /// <summary>The registered email address.</summary>
  public string Email { get; init; } = string.Empty;
}

public class RegisterEndpoint(IMediator mediator) : Ep.Req<RegisterRequest>.Res<RegisterResponse> 
{
  public override void Configure()
  {
    Post("/api/auth/register");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    var command = new RegisterCommand(
      req.Email,
      req.Password,
      req.FirstName,
      req.LastName);

    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(new RegisterResponse
      {
        CustomerId = result.Value.CustomerId,
        Email = result.Value.Email
      }, ct);
    }
    else
    {
      AddError(string.Join(", ", result.Errors));
      await SendErrorsAsync(400, ct);
    }
  }
}
