using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
	{
		private const int MaxCommentLength = 2000;
		private const int MinCommentLength = 10;

		public CreateReviewValidator()
		{
			RuleFor(x => x.OrderDetailId)
				.NotEmpty()
				.WithMessage("Order detail ID is required.");

			RuleFor(x => x.Rating)
				.InclusiveBetween(1, 5)
				.WithMessage("Rating must be between 1 and 5 stars.");

			RuleFor(x => x.Comment)
				.NotEmpty()
				.WithMessage("Comment is required.")
				.MinimumLength(MinCommentLength)
				.WithMessage($"Comment must be at least {MinCommentLength} characters.")
				.MaximumLength(MaxCommentLength)
				.WithMessage($"Comment must not exceed {MaxCommentLength} characters.");
		}
	}
}
