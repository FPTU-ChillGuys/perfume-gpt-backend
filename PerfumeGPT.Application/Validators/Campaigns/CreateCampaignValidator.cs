using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Promotions;

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
		}
	}

	public class CreateCampaignPromotionItemValidator : AbstractValidator<CreateCampaignPromotionItemRequest>
	{
		public CreateCampaignPromotionItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
				.NotEmpty().WithMessage("Product variant is required.");

			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Promotion item name is required.")
				.MaximumLength(100).WithMessage("Promotion item name must not exceed 100 characters.");

			RuleFor(x => x.EndDate)
				.GreaterThan(x => x.StartDate).WithMessage("Item end date must be after item start date.")
				.When(x => x.StartDate.HasValue && x.EndDate.HasValue);

			RuleFor(x => x.MaxUsage)
				.GreaterThan(0).WithMessage("Item max usage must be greater than 0.")
				.When(x => x.MaxUsage.HasValue);
		}
	}
}
