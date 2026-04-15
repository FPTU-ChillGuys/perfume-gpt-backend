using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;

namespace PerfumeGPT.Application.Validators.SourcingCatalogs
{
	public class CreateCatalogItemValidator : AbstractValidator<CreateCatalogItemRequest>
	{
		public CreateCatalogItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
				.NotEmpty().WithMessage("ProductVariantId is required.")
				.NotEqual(Guid.Empty).WithMessage("ProductVariantId must be a valid GUID.");

			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("SupplierId must be a positive integer.");

			RuleFor(x => x.NegotiatedPrice)
				.GreaterThan(0).WithMessage("NegotiatedPrice must be greater than 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
				.GreaterThanOrEqualTo(0).WithMessage("EstimatedLeadTimeDays cannot be negative.");
		}
	}
}
