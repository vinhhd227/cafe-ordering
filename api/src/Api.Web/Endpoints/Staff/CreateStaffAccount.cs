using Api.UseCases.Auth.CreateStaffAccount;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Staff;

public sealed class CreateStaffAccountRequest
{
  public string Username { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;

  /// <summary>Role to assign: "Admin" or "Staff".</summary>
  public string Role { get; set; } = "Staff";
}

public sealed class CreateStaffAccountResponse
{
  public string Username { get; init; } = string.Empty;
  public string TemporaryPassword { get; init; } = string.Empty;
}

public class CreateStaffAccountEndpoint(IMediator mediator)
  : Ep.Req<CreateStaffAccountRequest>.Res<CreateStaffAccountResponse>
{
  public override void Configure()
  {
    Post("/api/staff/accounts");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Staff"));
  }

  public override async Task HandleAsync(CreateStaffAccountRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new CreateStaffAccountCommand(req.Username, req.FullName, req.Role), ct);

    if (!result.IsSuccess)
    {
      var errMsg = string.Join("; ", result.Errors);
      await this.SendResultAsync(
        Result<CreateStaffAccountResponse>.Error(errMsg), ct);
      return;
    }

    await SendAsync(new CreateStaffAccountResponse
    {
      Username = result.Value.Username,
      TemporaryPassword = result.Value.TemporaryPassword
    }, 201, ct);
  }
}
