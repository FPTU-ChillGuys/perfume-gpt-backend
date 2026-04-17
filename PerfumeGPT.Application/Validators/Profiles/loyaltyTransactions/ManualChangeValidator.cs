using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Loyalty;

namespace PerfumeGPT.Application.Validators.Profiles.loyaltyTransactions
{
	public class ManualChangeValidator : AbstractValidator<ManualChangeRequest>
	{
		public ManualChangeValidator()
		{
			RuleFor(x => x.TransactionType)
				.IsInEnum()
              .WithMessage("Loại giao dịch không hợp lệ.");

			RuleFor(x => x.Points)
				.GreaterThan(0)
             .WithMessage("Số điểm phải lớn hơn 0.");

			RuleFor(x => x.Reason)
				.NotEmpty()
                .WithMessage("Lý do là bắt buộc khi thay đổi điểm thủ công.")
				.MaximumLength(500)
               .WithMessage("Lý do không được vượt quá 500 ký tự.");
		}
	}
}
