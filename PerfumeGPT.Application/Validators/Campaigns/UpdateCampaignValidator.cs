using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.Validators.Campaigns.PromotionItems;
using PerfumeGPT.Application.Validators.Campaigns.Vouchers;

namespace PerfumeGPT.Application.Validators.Campaigns
{
	public class UpdateCampaignValidator : AbstractValidator<UpdateCampaignRequest>
	{
		public UpdateCampaignValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Campaign name is required.")
				.MaximumLength(100).WithMessage("Campaign name must not exceed 100 characters.");

			RuleFor(x => x.EndDate)
				.GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

			RuleFor(x => x.Items)
				.NotEmpty().WithMessage("Campaign must include at least one promotion item.");

			RuleForEach(x => x.Items)
			  .SetValidator(new UpdateCampaignPromotionItemValidator());

			RuleForEach(x => x.Vouchers)
				.SetValidator(new UpdateCampaignVoucherValidator());

			RuleFor(x => x.Vouchers)
				.Must(vouchers => vouchers == null || vouchers.Select(v => v.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == vouchers.Count)
				.WithMessage("Voucher codes in campaign must be unique.");
		}
	}
}
