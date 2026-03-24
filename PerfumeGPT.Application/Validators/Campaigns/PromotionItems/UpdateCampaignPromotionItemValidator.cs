using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;

namespace PerfumeGPT.Application.Validators.Campaigns.PromotionItems
{
 public class UpdateCampaignPromotionItemValidator : AbstractValidator<UpdateCampaignPromotionItemRequest>
	{
       public UpdateCampaignPromotionItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
				.NotEmpty().WithMessage("ProductVariantId is required.")
				.Must(id => id != Guid.Empty).WithMessage("ProductVariantId must be a valid GUID.");

			RuleFor(x => x.PromotionType)
				.IsInEnum().WithMessage("ItemType must be a valid PromotionType.");

			RuleFor(x => x.MaxUsage)
				.GreaterThan(0).When(x => x.MaxUsage.HasValue).WithMessage("MaxUsage must be greater than 0 if specified.");
		}
	}
}
