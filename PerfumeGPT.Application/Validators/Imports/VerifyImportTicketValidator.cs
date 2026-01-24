using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.ImportDetails;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class VerifyImportTicketValidator : AbstractValidator<VerifyImportTicketRequest>
	{
		public VerifyImportTicketValidator()
		{
			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Import details are required.")
				.Must(details => details != null && details.Count > 0).WithMessage("At least one import detail is required.");

			RuleForEach(x => x.ImportDetails).SetValidator(new VerifyImportDetailValidator());
		}
	}
}
