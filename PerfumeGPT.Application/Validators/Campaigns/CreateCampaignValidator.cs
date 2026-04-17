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
				.NotEmpty().WithMessage("Tên chiến dịch là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên chiến dịch không được vượt quá 100 ký tự.");

			RuleFor(x => x.StartDate)
				.GreaterThanOrEqualTo(DateTime.UtcNow).WithMessage("Ngày bắt đầu phải là trong tương lai.");

			RuleFor(x => x.EndDate)
				.GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

			RuleFor(x => x.Items)
				.NotEmpty().WithMessage("Chiến dịch phải bao gồm ít nhất một mục khuyến mãi.");

			RuleForEach(x => x.Items)
				.SetValidator(new CreateCampaignPromotionItemValidator());

			RuleForEach(x => x.Vouchers)
				.SetValidator(new CreateCampaignVoucherValidator());

			RuleFor(x => x.Vouchers)
				.Must(vouchers => vouchers == null || vouchers.Select(v => v.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == vouchers.Count)
				.WithMessage("Mã voucher trong chiến dịch phải là duy nhất.");
		}
	}
}
