using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;

namespace PerfumeGPT.Application.Validators.Campaigns.PromotionItems
{
	public class CreateCampaignPromotionItemValidator : AbstractValidator<CreateCampaignPromotionItemRequest>
	{
		public CreateCampaignPromotionItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
				.NotEmpty().WithMessage("ProductVariantId là bắt buộc.")
				.Must(id => id != Guid.Empty).WithMessage("ProductVariantId phải là một GUID hợp lệ.");
			RuleFor(x => x.PromotionType)
				.IsInEnum().WithMessage("ItemType phải là một PromotionType hợp lệ.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("DiscountType phải là một DiscountType hợp lệ.");

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("DiscountValue phải lớn hơn 0.");

			RuleFor(x => x.DiscountValue)
				.LessThanOrEqualTo(100)
				.When(x => x.DiscountType == Domain.Enums.DiscountType.Percentage)
				.WithMessage("Percentage discount không được vượt quá 100%.");
		}
	}
}
