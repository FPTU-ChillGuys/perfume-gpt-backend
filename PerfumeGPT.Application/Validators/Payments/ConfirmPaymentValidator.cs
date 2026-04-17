using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Payments;

namespace PerfumeGPT.Application.Validators.Payments
{
	public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentRequest>
	{
		public ConfirmPaymentValidator()
		{
			RuleFor(x => x.IsSuccess)
             .NotNull().WithMessage("Bắt buộc cung cấp trạng thái thanh toán thành công.");
			When(x => !x.IsSuccess, () =>
			{
				RuleFor(x => x.FailureReason)
                 .NotEmpty().WithMessage("Bắt buộc cung cấp lý do thất bại khi thanh toán không thành công.");
			});

			When(x => x.IsSuccess, () =>
			{
				RuleFor(x => x.PosSessionId)
                 .NotEmpty().WithMessage("Bắt buộc cung cấp POS session ID khi thanh toán thành công.");
			});
		}
	}
}
