using Api.Core.Aggregates.SavedMenuAggregate;
using Api.Core.Aggregates.SavedMenuAggregate.Specifications;

namespace Api.UseCases.SavedMenus.Delete;

public class DeleteSavedMenuHandler(IRepositoryBase<SavedMenu> repository)
  : ICommandHandler<DeleteSavedMenuCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteSavedMenuCommand request, CancellationToken ct)
  {
    var menu = await repository.FirstOrDefaultAsync(new SavedMenuByIdSpec(request.Id), ct);

    if (menu is null)
      return Result.NotFound($"Saved menu {request.Id} not found.");

    menu.Delete(request.DeletedBy);
    await repository.UpdateAsync(menu, ct);

    return Result.Success();
  }
}
