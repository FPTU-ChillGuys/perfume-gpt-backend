using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Variants;

namespace PerfumeGPT.Application.Validators.Variants
{
	public class UpdateVariantValidator : AbstractValidator<UpdateVariantRequest>
	{
		public UpdateVariantValidator()
		{
			RuleFor(x => x.Sku)
				.NotEmpty().WithMessage("SKU is required.")
				.MaximumLength(50).WithMessage("SKU must not exceed 50 characters.");
			RuleFor(x => x.VolumeMl)
				.GreaterThan(0).WithMessage("Volume (ml) must be greater than 0.");
			RuleFor(x => x.ConcentrationId)
				.GreaterThan(0).WithMessage("ConcentrationId must be a positive integer.");
			RuleFor(x => x.BasePrice)
				.GreaterThanOrEqualTo(0).WithMessage("BasePrice must be greater than or equal to 0.");
		}
	}
}
