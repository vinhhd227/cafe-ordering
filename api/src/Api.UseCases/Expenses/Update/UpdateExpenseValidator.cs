using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.OrderAggregate;
using FluentValidation;

namespace Api.UseCases.Expenses.Update;

public class UpdateExpenseValidator : AbstractValidator<UpdateExpenseCommand>
{
  public UpdateExpenseValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Tên mặt hàng không được để trống")
      .MaximumLength(200).WithMessage("Tên mặt hàng không quá 200 ký tự");

    RuleFor(x => x.Category)
      .NotEmpty().WithMessage("Loại chi phí không được để trống")
      .Must(c => ExpenseCategory.TryFromName(c, true, out _))
      .WithMessage("Loại chi phí không hợp lệ. Các giá trị hợp lệ: INGREDIENT, SUPPLY, EQUIPMENT, OTHER");

    RuleFor(x => x.PaymentMethod)
      .NotEmpty().WithMessage("Phương thức thanh toán không được để trống")
      .Must(m => PaymentMethod.TryFromName(m, true, out _) &&
                 m.ToUpperInvariant() is "CASH" or "BANK_TRANSFER")
      .WithMessage("Phương thức thanh toán không hợp lệ. Các giá trị hợp lệ: CASH, BANK_TRANSFER");

    RuleFor(x => x.Quantity)
      .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0");

    RuleFor(x => x.Unit)
      .MaximumLength(50).WithMessage("Đơn vị không quá 50 ký tự")
      .When(x => x.Unit is not null);

    RuleFor(x => x.UnitPrice)
      .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm")
      .Must(p => p == Math.Floor(p)).WithMessage("Đơn giá phải là số nguyên");

    RuleFor(x => x.Notes)
      .MaximumLength(500).WithMessage("Ghi chú không quá 500 ký tự")
      .When(x => x.Notes is not null);
  }
}
