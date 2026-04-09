using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;

namespace PerfumeGPT.Application.Validators.Campaigns.PromotionItems
{
	public class CreateCampaignPromotionItemValidator : AbstractValidator<CreateCampaignPromotionItemRequest>
	{
		public CreateCampaignPromotionItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
				.NotEmpty().WithMessage("ProductVariantId is required.")
				.Must(id => id != Guid.Empty).WithMessage("ProductVariantId must be a valid GUID.");
			RuleFor(x => x.PromotionType)
				.IsInEnum().WithMessage("ItemType must be a valid PromotionType.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("DiscountType must be a valid DiscountType.");

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("DiscountValue must be greater than 0.");

			RuleFor(x => x.DiscountValue)
				.LessThanOrEqualTo(100)
				.When(x => x.DiscountType == Domain.Enums.DiscountType.Percentage)
				.WithMessage("Percentage discount cannot exceed 100%.");
		}
	}
}
