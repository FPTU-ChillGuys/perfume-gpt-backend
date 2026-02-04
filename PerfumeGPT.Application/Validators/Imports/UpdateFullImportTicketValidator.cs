using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class UpdateFullImportTicketValidator : AbstractValidator<UpdateFullImportTicketRequest>
	{
		public UpdateFullImportTicketValidator()
		{
			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("Supplier ID must be a positive integer.");

			RuleFor(x => x.ExpectedArrivalDate)
				.NotEmpty().WithMessage("Expected arrival date is required.");

			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Import details are required.")
				.Must(details => details != null && details.Count > 0).WithMessage("At least one import detail is required.");

			RuleForEach(x => x.ImportDetails).SetValidator(new UpdateImportDetailValidator());
		}
	}

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
