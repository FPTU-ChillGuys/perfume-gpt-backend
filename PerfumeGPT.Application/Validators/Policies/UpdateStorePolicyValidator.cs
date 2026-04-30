using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StorePolicies;

namespace PerfumeGPT.Application.Validators.Policies
{
	public class UpdateStorePolicyValidator : AbstractValidator<UpdateStorePolicyRequest>
	{
		public UpdateStorePolicyValidator()
		{
			RuleFor(x => x.RequiredDepositPercentage)
				.InclusiveBetween(0, 100)
				.WithMessage("Phần trăm cọc phải nằm trong khoảng từ 0 đến 100.");
			RuleFor(x => x.DepositTimeoutMinutes)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Thời gian hết hạn cọc phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.ReviewRewardPoints)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Điểm thưởng đánh giá phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.StockAdjustmentAutoApprovalThreshold)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Ngưỡng tự động duyệt điều chỉnh kho phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.MaxAddressesPerUser)
				.GreaterThan(0)
				.WithMessage("Số lượng địa chỉ tối đa mỗi người dùng phải lớn hơn 0.");
		}
	}
}
