using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.Batches;

namespace PerfumeGPT.Application.Validators.ImportDetails
{
	public class VerifyImportDetailValidator : AbstractValidator<VerifyImportDetailRequest>
	{
		public VerifyImportDetailValidator()
		{
			RuleFor(x => x.ImportDetailId)
				.NotEmpty().WithMessage("Import detail ID is required.");

			RuleFor(x => x.RejectQuantity)
				.GreaterThanOrEqualTo(0).WithMessage("Reject quantity cannot be negative.");

			RuleFor(x => x.Note)
				.MaximumLength(500).WithMessage("Note must not exceed 500 characters.");

			RuleFor(x => x.Batches)
				.NotEmpty().WithMessage("Batches are required.")
				.Must(batches => batches != null && batches.Count > 0).WithMessage("At least one batch is required.");

			RuleForEach(x => x.Batches).SetValidator(new CreateBatchValidator());
		}
	}
}
