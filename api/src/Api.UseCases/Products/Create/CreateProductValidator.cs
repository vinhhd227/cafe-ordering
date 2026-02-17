using FluentValidation;

namespace Api.UseCases.Products.Create;

/// <summary>
///   Validator cho CreateProductCommand
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
  public CreateProductValidator()
  {
    RuleFor(x => x.CategoryId)
      .GreaterThan(0).WithMessage("CategoryId không hợp lệ");

    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
      .MaximumLength(200).WithMessage("Tên sản phẩm không quá 200 ký tự");

    RuleFor(x => x.Description)
      .MaximumLength(2000).WithMessage("Mô tả không quá 2000 ký tự")
      .When(x => x.Description is not null);

    RuleFor(x => x.Price)
      .GreaterThan(0).WithMessage("Giá phải lớn hơn 0");

    RuleFor(x => x.ImageUrl)
      .MaximumLength(500).WithMessage("URL ảnh không quá 500 ký tự")
      .When(x => x.ImageUrl is not null);
  }
}
