using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class ModerateReviewValidator : AbstractValidator<ModerateReviewRequest>
	{
		private const int MaxReasonLength = 500;

		public ModerateReviewValidator()
		{
			RuleFor(x => x.Status)
				.Must(status => status == ReviewStatus.Approved || status == ReviewStatus.Rejected)
				.WithMessage("Review status must be either Approved or Rejected.");

			RuleFor(x => x.ModerationReason)
				.MaximumLength(MaxReasonLength)
				.WithMessage($"Moderation reason must not exceed {MaxReasonLength} characters.");

			RuleFor(x => x.ModerationReason)
				.NotEmpty()
				.WithMessage("Moderation reason is required when rejecting a review.")
				.When(x => x.Status == ReviewStatus.Rejected);
		}
	}
}
