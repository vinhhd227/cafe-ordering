using System.Security.Claims;
using Api.UseCases.SavedMenus.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.SavedMenus;

public sealed class DeleteSavedMenuRequest
{
  public int Id { get; set; }
}

public class DeleteSavedMenu(IMediator mediator) : Ep.Req<DeleteSavedMenuRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/saved-menus/{Id}");
    Policies("utility.delete");
    DontAutoTag();
    Description(b => b.WithTags("Saved Menus"));
  }

  public override async Task HandleAsync(DeleteSavedMenuRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteSavedMenuCommand(req.Id, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}
