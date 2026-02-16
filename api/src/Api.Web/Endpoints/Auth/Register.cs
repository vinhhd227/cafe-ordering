using Api.UseCases.Auth.Register;
using MediatR;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Register endpoint request DTO
/// </summary>
public record RegisterRequest(
  string Email,
  string Password,
  string FirstName,
  string LastName);

/// <summary>
/// Register endpoint response DTO
/// </summary>
public record RegisterResponseDto(string CustomerId, string Email);

/// <summary>
/// FastEndpoints endpoint for user registration.
/// Creates Customer aggregate and ApplicationUser via MediatR.
/// </summary>
public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponseDto>
{
  private readonly ISender _mediator;

  public RegisterEndpoint(ISender mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Post("/api/auth/register");
    AllowAnonymous();
    Description(b => b
      .WithTags("Auth")
      .WithSummary("Register a new customer")
      .WithDescription("Create a new customer account with authentication credentials"));
  }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    var command = new RegisterCommand(
      req.Email,
      req.Password,
      req.FirstName,
      req.LastName);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(new RegisterResponseDto(
        result.Value.CustomerId,
        result.Value.Email), ct);
    }
    else
    {
      await SendAsync(new
      {
        Message = string.Join(", ", result.Errors)
      }, 400, ct);
    }
  }
}
