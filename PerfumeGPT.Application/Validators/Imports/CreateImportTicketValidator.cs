using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class CreateImportTicketValidator : AbstractValidator<CreateImportTicketRequest>
	{
		public CreateImportTicketValidator()
		{
			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("ID nhà cung cấp phải lớn hơn 0.");

			RuleFor(x => x.ExpectedArrivalDate)
				.NotEmpty().WithMessage("Ngày dự kiến đến là bắt buộc.");

			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Chi tiết nhập hàng là bắt buộc.")
				.Must(details => details != null && details.Count > 0).WithMessage("Phải có ít nhất một chi tiết nhập hàng.");
			RuleForEach(x => x.ImportDetails).SetValidator(new CreateImportDetailValidator());
		}
	}
}
