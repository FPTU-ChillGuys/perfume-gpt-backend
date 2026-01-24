using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.ImportDetails;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class CreateImportTicketValidator : AbstractValidator<CreateImportTicketRequest>
	{
		public CreateImportTicketValidator()
		{
			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("Supplier ID must be a positive integer.");

			RuleFor(x => x.ImportDate)
				.NotEmpty().WithMessage("Import date is required.")
				.LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Import date cannot be in the future.");

			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Import details are required.")
				.Must(details => details != null && details.Count > 0).WithMessage("At least one import detail is required.");

			RuleForEach(x => x.ImportDetails).SetValidator(new CreateImportDetailValidator());
		}
	}
}
