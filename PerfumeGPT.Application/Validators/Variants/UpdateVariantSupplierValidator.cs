using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Variants;

namespace PerfumeGPT.Application.Validators.Variants
{
	public class UpdateVariantSupplierValidator : AbstractValidator<UpdateVariantSupplierRequest>
	{
		public UpdateVariantSupplierValidator()
		{
			RuleFor(x => x.NegotiatedPrice)
				.GreaterThan(0).WithMessage("NegotiatedPrice must be greater than 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
				.GreaterThanOrEqualTo(0).WithMessage("EstimatedLeadTimeDays cannot be negative.");
		}
	}
}
