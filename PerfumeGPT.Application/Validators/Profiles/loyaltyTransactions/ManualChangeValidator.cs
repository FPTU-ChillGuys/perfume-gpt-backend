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
				.WithMessage("Invalid transaction type.");

			RuleFor(x => x.Points)
				.GreaterThan(0)
				.WithMessage("Points must be greater than 0.");

			RuleFor(x => x.Reason)
				.NotEmpty()
				.WithMessage("Reason is required for manual point changes.")
				.MaximumLength(500)
				.WithMessage("Reason cannot exceed 500 characters.");
		}
	}
}
