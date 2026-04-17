using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Validators.Imports.ImportDetails;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class VerifyImportTicketValidator : AbstractValidator<VerifyImportTicketRequest>
	{
		public VerifyImportTicketValidator()
		{
			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Chi tiết nhập hàng là bắt buộc.")
				.Must(details => details != null && details.Count > 0).WithMessage("Phải có ít nhất một chi tiết nhập hàng.");

			RuleForEach(x => x.ImportDetails).SetValidator(new VerifyImportDetailValidator());
		}
	}
}
