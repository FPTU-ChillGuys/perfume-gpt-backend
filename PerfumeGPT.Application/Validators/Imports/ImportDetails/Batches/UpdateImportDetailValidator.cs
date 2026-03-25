using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches
{
	public class UpdateImportDetailValidator : AbstractValidator<UpdateImportDetailRequest>
	{
		public UpdateImportDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID is required.");

			RuleFor(x => x.ExpectedQuantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than zero.");

			RuleFor(x => x.UnitPrice)
				.GreaterThanOrEqualTo(0).WithMessage("Unit price must be greater than or equal to zero.");
		}
	}
}
