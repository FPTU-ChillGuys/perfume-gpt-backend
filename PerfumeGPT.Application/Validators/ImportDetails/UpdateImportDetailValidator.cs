using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ImportDetails;

namespace PerfumeGPT.Application.Validators.ImportDetails
{
	public class UpdateImportDetailValidator : AbstractValidator<UpdateImportDetailRequest>
	{
		public UpdateImportDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID is required.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than zero.");

			RuleFor(x => x.UnitPrice)
				.GreaterThanOrEqualTo(0).WithMessage("Unit price must be greater than or equal to zero.");
		}
	}
}
