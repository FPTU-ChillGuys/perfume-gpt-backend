using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;

namespace PerfumeGPT.Application.Validators.Campaigns.Vouchers
{
	public class UpdateCampaignVoucherValidator : AbstractValidator<UpdateCampaignVoucherRequest>
	{
		public UpdateCampaignVoucherValidator()
		{
			RuleFor(x => x.Code)
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.Matches("^[A-Z0-9_-]+$").WithMessage("Voucher code must contain only uppercase letters, numbers, hyphens, and underscores.")
				.When(x => !string.IsNullOrEmpty(x.Code));

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("Discount value must be greater than 0.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("Invalid discount type.");

			RuleFor(x => x.ApplyType)
				.IsInEnum().WithMessage("Invalid apply type.");

			RuleFor(x => x.TargetItemType)
				.IsInEnum().WithMessage("Invalid target item type.");

			RuleFor(x => x.MinOrderValue)
				.GreaterThanOrEqualTo(0).WithMessage("Minimum order value must be greater than or equal to 0.");

			RuleFor(x => x.MaxDiscountAmount)
				.GreaterThan(0).WithMessage("Max discount amount must be greater than 0.")
				.When(x => x.MaxDiscountAmount.HasValue);

			RuleFor(x => x.TotalQuantity)
				.GreaterThan(0).WithMessage("Total quantity must be greater than 0.")
				.When(x => x.TotalQuantity.HasValue);

			RuleFor(x => x.MaxUsagePerUser)
				.GreaterThan(0).WithMessage("Max usage per user must be greater than 0.")
				.When(x => x.MaxUsagePerUser.HasValue);
		}
	}
}
