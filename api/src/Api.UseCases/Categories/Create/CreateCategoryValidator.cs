using FluentValidation;

namespace Api.UseCases.Categories.Create;

/// <summary>
///   Validator cho CreateCategoryCommand
/// </summary>
public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
  public CreateCategoryValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Tên danh mục không được để trống")
      .MaximumLength(100).WithMessage("Tên danh mục không quá 100 ký tự");
  }
}
