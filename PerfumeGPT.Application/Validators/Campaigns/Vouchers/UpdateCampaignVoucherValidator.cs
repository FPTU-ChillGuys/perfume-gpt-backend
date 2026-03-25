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
		}
	}
}
