using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Media;

namespace PerfumeGPT.Application.Validators.Media
{
	public class ProductUploadMediaValidator : AbstractValidator<ProductUploadMediaRequest>
	{
		public ProductUploadMediaValidator()
		{
			RuleFor(x => x.Images)
				.NotEmpty().WithMessage("At least one image is required.");

			RuleFor(x => x.Images).Custom((images, context) =>
			{
				if (images == null) return;

				// Ensure at most one primary image
				var primaryCount = images.Count(i => i.IsPrimary);
				if (primaryCount > 1)
				{
					context.AddFailure("Only one image can be marked as primary.");
				}

				// Ensure no duplicate display orders
				var duplicateOrders = images.GroupBy(i => i.DisplayOrder)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateOrders.Count != 0)
				{
					context.AddFailure($"Duplicate display order values found: {string.Join(", ", duplicateOrders)}");
				}
			});

			// Validate each image item
			RuleForEach(x => x.Images).ChildRules(item =>
			{
				item.RuleFor(i => i.ImageFile).NotNull().WithMessage("ImageFile is required.");
				item.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("DisplayOrder must be >= 0.");
			});
		}
	}
}
