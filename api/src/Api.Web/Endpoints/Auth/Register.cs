// Deprecated: use /api/client/auth/register instead.
using Api.UseCases.Auth.Register;
using FluentValidation;
using Mediator;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Request payload for registering a new customer account.
/// </summary>
public sealed class RegisterRequest
{
  /// <summary>Chosen username. Used as the login identifier and must be unique.</summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>Valid email address for contact purposes.</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// Password must be at least 8 characters and contain at least one uppercase letter,
  /// one lowercase letter, one digit, and one non-alphanumeric character.
  /// </summary>
  public string Password { get; set; } = string.Empty;

  /// <summary>Customer's full name (e.g. "Ava Nguyen").</summary>
  public string FullName { get; set; } = string.Empty;
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

sealed class RegisterRequestValidator : Validator<RegisterRequest>
{
  public RegisterRequestValidator()
  {
    RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required");
    RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Invalid email address");
    RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required");
  }
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
      req.Username,
      req.Email,
      req.Password,
      req.FullName);

    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await Send.OkAsync(new RegisterResponse
      {
        CustomerId = result.Value.CustomerId,
        Email = result.Value.Email
      }, ct);
    }
    else
    {
      AddError(string.Join(", ", result.Errors));
      await Send.ErrorsAsync(400, ct);
    }
  }
}
