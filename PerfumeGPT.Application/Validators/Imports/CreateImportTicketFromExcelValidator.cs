using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class CreateImportTicketFromExcelValidator : AbstractValidator<UploadImportTicketFromExcelRequest>
	{
		public CreateImportTicketFromExcelValidator()
		{
			RuleFor(x => x.ExcelFile)
				.NotNull().WithMessage("Tệp Excel là bắt buộc.");

			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("Supplier ID phải lớn hơn 0.");

			RuleFor(x => x.ExpectedArrivalDate)
				.NotEmpty().WithMessage("Ngày dự kiến đến là bắt buộc.");
		}
	}
}
