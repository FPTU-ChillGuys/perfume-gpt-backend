using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class CreateImportTicketFromExcelValidator : AbstractValidator<CreateImportTicketFromExcelRequest>
	{
		public CreateImportTicketFromExcelValidator()
		{
			RuleFor(x => x.ExcelFile)
				.NotNull().WithMessage("Excel file is required.");

			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("Supplier ID must be greater than 0.");

			RuleFor(x => x.ExpectedArrivalDate)
				.NotEmpty().WithMessage("Expected arrival date is required.");
		}
	}
}
