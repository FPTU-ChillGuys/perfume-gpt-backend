using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.Validators.Campaigns.PromotionItems;
using PerfumeGPT.Application.Validators.Campaigns.Vouchers;

namespace PerfumeGPT.Application.Validators.Campaigns
{
	public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
	{
		public CreateCampaignValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Campaign name is required.")
				.MaximumLength(100).WithMessage("Campaign name must not exceed 100 characters.");

			RuleFor(x => x.StartDate)
				.GreaterThanOrEqualTo(DateTime.UtcNow).WithMessage("Start date must be in the future.");

			RuleFor(x => x.EndDate)
				.GreaterThan(x => x.StartDate).WithMessage("End date must be after the start date.");

			RuleFor(x => x.Items)
				.NotEmpty().WithMessage("Campaign must include at least one promotion item.");

			RuleForEach(x => x.Items)
				.SetValidator(new CreateCampaignPromotionItemValidator());

            RuleForEach(x => x.Vouchers)
				.SetValidator(new CreateCampaignVoucherValidator());

			RuleFor(x => x.Vouchers)
                .Must(vouchers => vouchers == null || vouchers.Select(v => v.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == vouchers.Count)
				.WithMessage("Voucher codes in campaign must be unique.");
		}
	}
}
