using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.ImportDetails
{
	public class CreateImportDetailValidator : AbstractValidator<CreateImportDetailRequest>
	{
		public CreateImportDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID is required.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than 0.");

			RuleFor(x => x.UnitPrice)
				.GreaterThan(0).WithMessage("Unit price must be greater than 0.");
		}
	}
}
