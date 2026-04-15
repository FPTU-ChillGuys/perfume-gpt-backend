using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Payments;

namespace PerfumeGPT.Application.Validators.Payments
{
	public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentRequest>
	{
		public ConfirmPaymentValidator()
		{
			RuleFor(x => x.IsSuccess)
				.NotNull().WithMessage("Payment success status must be provided.");
			When(x => !x.IsSuccess, () =>
			{
				RuleFor(x => x.FailureReason)
					.NotEmpty().WithMessage("Failure reason must be provided when payment is not successful.");
			});

			When(x => x.IsSuccess, () =>
			{
				RuleFor(x => x.PosSessionId)
					.NotEmpty().WithMessage("POS session ID must be provided when payment is successful.");
			});
		}
	}
}
