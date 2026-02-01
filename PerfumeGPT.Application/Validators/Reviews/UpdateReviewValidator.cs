using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class UpdateReviewValidator : AbstractValidator<UpdateReviewRequest>
	{
		private const int MaxCommentLength = 2000;
		private const int MinCommentLength = 10;
		private const int MaxImageCount = 10; // Total images allowed per review

		public UpdateReviewValidator()
		{
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

			// Validate image lists
			RuleFor(x => x.TemporaryMediaIdsToAdd)
				.Must(ids => ids == null || ids.Count <= MaxImageCount)
				.WithMessage($"You can add a maximum of {MaxImageCount} images at once.");

			RuleFor(x => x.MediaIdsToDelete)
				.Must(ids => ids == null || ids.Count <= MaxImageCount)
				.WithMessage($"You can delete a maximum of {MaxImageCount} images at once.");
		}
	}
}

