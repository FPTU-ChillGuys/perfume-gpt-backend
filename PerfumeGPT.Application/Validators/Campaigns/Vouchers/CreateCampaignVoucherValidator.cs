using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Validators.Campaigns.Vouchers
{
	public class CreateCampaignVoucherValidator : AbstractValidator<CreateCampaignVoucherRequest>
	{
		public CreateCampaignVoucherValidator()
		{
			RuleFor(x => x.Code)
				.NotEmpty().WithMessage("Mã voucher là bắt buộc.")
				.MaximumLength(50).WithMessage("Mã voucher không được vượt quá 50 ký tự.")
				.Matches("^[A-Z0-9_-]+$").WithMessage("Mã voucher chỉ được chứa chữ hoa, số, dấu gạch ngang và dấu gạch dưới.");

			RuleFor(x => x.DiscountValue)
				.GreaterThan(0).WithMessage("Giá trị giảm giá phải lớn hơn 0.");

			RuleFor(x => x.DiscountType)
				.IsInEnum().WithMessage("Loại giảm giá không hợp lệ.");

			RuleFor(x => x.ApplyType)
				.IsInEnum().WithMessage("Loại áp dụng không hợp lệ.");

			RuleFor(x => x.TargetItemType)
			.NotNull().WithMessage("Bắt buộc chọn loại mục tiêu (TargetItemType) khi áp dụng voucher cho sản phẩm.")
			.IsInEnum().WithMessage("Loại mục tiêu không hợp lệ.")
			.When(x => x.ApplyType == VoucherType.Product);

			// RÀNG BUỘC CHO VOUCHER TOÀN ĐƠN (Phải là null)
			RuleFor(x => x.TargetItemType)
				.Null().WithMessage("Voucher áp dụng toàn đơn hàng không được có loại mục tiêu (TargetItemType).")
				.When(x => x.ApplyType == VoucherType.Order);

			RuleFor(x => x.MinOrderValue)
				.GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0.");

			RuleFor(x => x.MaxDiscountAmount)
				.GreaterThan(0).WithMessage("Giá trị giảm giá tối đa phải lớn hơn 0.")
				.When(x => x.MaxDiscountAmount.HasValue);

			RuleFor(x => x.TotalQuantity)
				.GreaterThan(0).WithMessage("Tổng số lượng phải lớn hơn 0.")
				.When(x => x.TotalQuantity.HasValue);

			RuleFor(x => x.MaxUsagePerUser)
				.GreaterThan(0).WithMessage("Số lần sử dụng tối đa cho mỗi người dùng phải lớn hơn 0.")
				.When(x => x.MaxUsagePerUser.HasValue);
		}
	}
}
