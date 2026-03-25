using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class UpdateImportValidator : AbstractValidator<UpdateImportRequest>
	{
		public UpdateImportValidator()
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
}
