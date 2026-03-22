using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Validators.ProductAttributes
{
	public class CreateAttributeValueValidator : AbstractValidator<CreateAttributeValueRequest>
	{
		public CreateAttributeValueValidator()
		{
			RuleFor(x => x.Value)
				.NotEmpty().WithMessage("Value is required.")
				.MaximumLength(200).WithMessage("Value must not exceed 200 characters.");
		}
	}
}
