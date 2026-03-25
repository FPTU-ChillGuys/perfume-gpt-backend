using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class AnswerReviewValidator : AbstractValidator<AnswerReviewRequest>
	{
		private const int MaxCommentLength = 2000;
		private const int MinCommentLength = 2;

		public AnswerReviewValidator()
		{
			RuleFor(x => x.StaffFeedbackComment)
				.NotEmpty()
				.WithMessage("Staff feedback comment is required.")
				.MinimumLength(MinCommentLength)
				.WithMessage($"Staff feedback comment must be at least {MinCommentLength} characters.")
				.MaximumLength(MaxCommentLength)
				.WithMessage($"Staff feedback comment must not exceed {MaxCommentLength} characters.");
		}
	}
}
