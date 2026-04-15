using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;

namespace PerfumeGPT.Application.Validators.SourcingCatalogs
{
	public class UpdateCatalogItemValidator : AbstractValidator<UpdateCatalogItemRequest>
	{
		public UpdateCatalogItemValidator()
		{
			RuleFor(x => x.NegotiatedPrice)
				.GreaterThan(0).WithMessage("NegotiatedPrice must be greater than 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
				.GreaterThanOrEqualTo(0).WithMessage("EstimatedLeadTimeDays cannot be negative.");
		}
	}
}
