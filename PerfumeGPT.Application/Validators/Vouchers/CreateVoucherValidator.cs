using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;

namespace PerfumeGPT.Application.Validators.Vouchers
{
	public class CreateVoucherValidator : AbstractValidator<CreateVoucherRequest>
	{
		public CreateVoucherValidator()
		{
			RuleFor(x => x.Code)
				.NotEmpty().WithMessage("Voucher code is required.")
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.Matches("^[A-Z0-9_-]+$").WithMessage("Voucher code must contain only uppercase letters, numbers, hyphens, and underscores.");

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("Discount value must be greater than 0.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("Invalid discount type.");

			RuleFor(x => x.ApplyType)
				.IsInEnum().WithMessage("Invalid apply type.");

			RuleFor(x => x.RequiredPoints)
				.GreaterThanOrEqualTo(0).WithMessage("Required points must be greater than or equal to 0.");

			RuleFor(x => x.MaxDiscountAmount)
				.GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue)
				.WithMessage("Max discount amount must be greater than 0.");

			RuleFor(x => x.MinOrderValue)
				.GreaterThanOrEqualTo(0).WithMessage("Minimum order value must be greater than or equal to 0.");

			RuleFor(x => x.ExpiryDate)
				.GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future.");

			RuleFor(x => x.DiscountValue)
				.LessThanOrEqualTo(100)
				.When(x => x.DiscountType == Domain.Enums.DiscountType.Percentage)
				.WithMessage("Percentage discount cannot exceed 100%.");

			RuleFor(x => x.TotalQuantity)
				.GreaterThan(0).WithMessage("Total quantity must be greater than 0.");

			RuleFor(x => x.MaxUsagePerUser)
				.GreaterThan(0).When(x => x.MaxUsagePerUser.HasValue)
				.WithMessage("Max usage per user must be greater than 0.");
		}
	}
}
