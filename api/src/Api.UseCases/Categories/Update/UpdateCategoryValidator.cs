using FluentValidation;

namespace Api.UseCases.Categories.Update;

/// <summary>
///   Validator cho UpdateCategoryCommand
/// </summary>
public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
  public UpdateCategoryValidator()
  {
    RuleFor(x => x.CategoryId)
      .GreaterThan(0).WithMessage("CategoryId không hợp lệ");

    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Tên danh mục không được để trống")
      .MaximumLength(100).WithMessage("Tên danh mục không quá 100 ký tự");
  }
}
