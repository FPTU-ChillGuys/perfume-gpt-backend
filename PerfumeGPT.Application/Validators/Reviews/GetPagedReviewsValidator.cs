using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class GetPagedReviewsValidator : AbstractValidator<GetPagedReviewsRequest>
	{
		public GetPagedReviewsValidator()
		{
			RuleFor(x => x.MinRating)
				.InclusiveBetween(1, 5)
				.WithMessage("Minimum rating must be between 1 and 5.")
				.When(x => x.MinRating.HasValue);

			RuleFor(x => x.MaxRating)
				.InclusiveBetween(1, 5)
				.WithMessage("Maximum rating must be between 1 and 5.")
				.When(x => x.MaxRating.HasValue);

			RuleFor(x => x)
				.Must(x => !x.MinRating.HasValue || !x.MaxRating.HasValue || x.MinRating <= x.MaxRating)
				.WithMessage("Minimum rating cannot be greater than maximum rating.")
				.When(x => x.MinRating.HasValue && x.MaxRating.HasValue);
		}
	}
}
