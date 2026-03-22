using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Validators.Attributes
{
	public class UpdateAttributeValidator : AbstractValidator<UpdateAttributeRequest>
	{
		public UpdateAttributeValidator()
		{
			RuleFor(x => x.InternalCode)
				.NotEmpty().WithMessage("InternalCode is required")
				.Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("InternalCode can only contain letters, numbers, underscores, and hyphens")
				.MaximumLength(50).WithMessage("InternalCode must not exceed 50 characters");

			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Name is required")
				.MaximumLength(100).WithMessage("Name must not exceed 100 characters");

			RuleFor(x => x.Description)
				.MaximumLength(500).WithMessage("Description must not exceed 500 characters");

			RuleFor(x => x.IsVariantLevel)
				.NotNull().WithMessage("IsVariantLevel is required");
		}
	}
}
