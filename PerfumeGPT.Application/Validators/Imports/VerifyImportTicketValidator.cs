using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class VerifyImportTicketValidator : AbstractValidator<VerifyImportTicketRequest>
	{
		public VerifyImportTicketValidator()
		{
			RuleFor(x => x.ImportTicketId)
				.NotEmpty().WithMessage("Import ticket ID is required.");

			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Import details are required.")
				.Must(details => details != null && details.Count > 0).WithMessage("At least one import detail is required.");

			RuleForEach(x => x.ImportDetails).SetValidator(new VerifyImportDetailValidator());
		}
	}

	public class VerifyImportDetailValidator : AbstractValidator<VerifyImportDetailRequest>
	{
		public VerifyImportDetailValidator()
		{
			RuleFor(x => x.ImportDetailId)
				.NotEmpty().WithMessage("Import detail ID is required.");

			RuleFor(x => x.Batches)
				.NotEmpty().WithMessage("Batches are required.")
				.Must(batches => batches != null && batches.Count > 0).WithMessage("At least one batch is required.");

			RuleForEach(x => x.Batches).SetValidator(new CreateBatchValidator());
		}
	}
}
