using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;

namespace PerfumeGPT.Application.Validators.Vouchers
{
	public class UpdateVoucherValidator : AbstractValidator<UpdateVoucherRequest>
	{
		public UpdateVoucherValidator()
		{
			RuleFor(x => x.Code)
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.Matches("^[A-Z0-9_-]+$").WithMessage("Voucher code must contain only uppercase letters, numbers, hyphens, and underscores.")
				.When(x => !string.IsNullOrEmpty(x.Code));

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("Discount value must be greater than 0.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("Invalid discount type.");

			RuleFor(x => x.RequiredPoints)
				.GreaterThanOrEqualTo(0).WithMessage("Required points must be greater than or equal to 0.");

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

			RuleFor(x => x.RemainingQuantity)
				.GreaterThanOrEqualTo(0).WithMessage("Remaining quantity must be greater than or equal to 0.");

			RuleFor(x => x)
				.Must(x => x.RemainingQuantity <= x.TotalQuantity)
				.WithMessage("Remaining quantity cannot exceed total quantity.");
		}
	}
}
