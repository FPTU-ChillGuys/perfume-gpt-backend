using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Validators.ProductAttributes
{
	public class UpdateAttributeValidator : AbstractValidator<UpdateAttributeRequest>
	{
		public UpdateAttributeValidator()
		{
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
