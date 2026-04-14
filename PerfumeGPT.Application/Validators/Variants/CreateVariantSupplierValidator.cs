using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Variants;

namespace PerfumeGPT.Application.Validators.Variants
{
	public class CreateVariantSupplierValidator : AbstractValidator<CreateVariantSupplierRequest>
	{
		public CreateVariantSupplierValidator()
		{
			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("SupplierId must be a positive integer.");

			RuleFor(x => x.NegotiatedPrice)
				.GreaterThan(0).WithMessage("NegotiatedPrice must be greater than 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
				.GreaterThanOrEqualTo(0).WithMessage("EstimatedLeadTimeDays cannot be negative.");
		}
	}
}
