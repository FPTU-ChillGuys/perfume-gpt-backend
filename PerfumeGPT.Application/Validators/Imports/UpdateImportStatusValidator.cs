using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class UpdateImportStatusValidator : AbstractValidator<UpdateImportStatusRequest>
	{
		public UpdateImportStatusValidator()
		{
			RuleFor(x => x.Status)
				.IsInEnum().WithMessage("Trạng thái không hợp lệ. Vui lòng chọn một trong các giá trị: Pending, InProgress, Completed, Cancelled.");
		}
	}
}
