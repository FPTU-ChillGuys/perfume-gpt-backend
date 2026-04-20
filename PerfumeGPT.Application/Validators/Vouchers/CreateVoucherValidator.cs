using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;

namespace PerfumeGPT.Application.Validators.Vouchers
{
	public class CreateVoucherValidator : AbstractValidator<CreateVoucherRequest>
	{
		public CreateVoucherValidator()
		{
			RuleFor(x => x.Code)
				.NotEmpty().WithMessage("Bắt buộc có mã giảm giá.")
				.MaximumLength(50).WithMessage("Mã giảm giá không được vượt quá 50 ký tự.")
				.Matches("^[A-Z0-9_-]+$").WithMessage("Mã giảm giá chỉ được chứa chữ in hoa, số, dấu gạch ngang và gạch dưới.");

			RuleFor(x => x.DiscountValue)
			  .GreaterThan(0).WithMessage("Giá trị giảm giá phải lớn hơn 0.");

			RuleFor(x => x.DiscountType)
			  .IsInEnum().WithMessage("Loại giảm giá không hợp lệ.");

			RuleFor(x => x.RequiredPoints)
				.GreaterThanOrEqualTo(0).WithMessage("Điểm yêu cầu phải lớn hơn hoặc bằng 0.");

			RuleFor(x => x.MaxDiscountAmount)
				.GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue)
				.WithMessage("Mức giảm tối đa phải lớn hơn 0.");

			RuleFor(x => x.MinOrderValue)
				.GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0.");

			RuleFor(x => x.ExpiryDate)
				.GreaterThan(DateTime.UtcNow).WithMessage("Ngày hết hạn phải ở tương lai.");

			RuleFor(x => x.DiscountValue)
				.LessThanOrEqualTo(100)
				.When(x => x.DiscountType == Domain.Enums.DiscountType.Percentage)
				.WithMessage("Mức giảm theo phần trăm không được vượt quá 100%.");

			RuleFor(x => x.TotalQuantity)
			  .GreaterThan(0).WithMessage("Tổng số lượng phải lớn hơn 0.");

			RuleFor(x => x.MaxUsagePerUser)
				.GreaterThan(0).When(x => x.MaxUsagePerUser.HasValue)
			 .WithMessage("Số lần sử dụng tối đa mỗi người dùng phải lớn hơn 0.");
		}
	}
}
