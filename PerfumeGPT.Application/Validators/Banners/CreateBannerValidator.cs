using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Banners;

namespace PerfumeGPT.Application.Validators.Banners
{
	public class CreateBannerValidator : AbstractValidator<CreateBannerRequest>
	{
		public CreateBannerValidator()
		{
			RuleFor(x => x.Title)
				.Must(title => !string.IsNullOrWhiteSpace(title))
				.WithMessage("Title is required.")
				.MaximumLength(200)
				.WithMessage("Title must not exceed 200 characters.");

			RuleFor(x => x.TemporaryImageId)
				.NotEmpty()
				.WithMessage("TemporaryImageId is required.");

			RuleFor(x => x.TemporaryMobileImageId)
				.Must(id => !id.HasValue || id.Value != Guid.Empty)
				.WithMessage("TemporaryMobileImageId must be a valid guid.");

			RuleFor(x => x)
				.Must(x => !x.TemporaryMobileImageId.HasValue || x.TemporaryMobileImageId.Value != x.TemporaryImageId)
				.WithMessage("Desktop and mobile temporary images must be different.");

			RuleFor(x => x.LinkTarget)
				.Must(target => !string.IsNullOrWhiteSpace(target))
				.WithMessage("Link target is required.");

			RuleFor(x => x.DisplayOrder)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Display order must be greater than or equal to 0.");

			RuleFor(x => x.Position)
				.IsInEnum().WithMessage("Invalid banner position.");

			RuleFor(x => x.LinkType)
				.IsInEnum().WithMessage("Invalid banner link type.");

			RuleFor(x => x)
				.Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate < x.EndDate)
				.WithMessage("End date must be after start date.");
		}
	}
}
