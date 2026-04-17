using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;
using PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches;

namespace PerfumeGPT.Application.Validators.Imports.ImportDetails
{
	public class VerifyImportDetailValidator : AbstractValidator<VerifyImportDetailRequest>
	{
		public VerifyImportDetailValidator()
		{
			RuleFor(x => x.ImportDetailId)
				.NotEmpty().WithMessage("ID chi tiết nhập hàng là bắt buộc.");

			RuleFor(x => x.RejectedQuantity)
				.GreaterThanOrEqualTo(0).WithMessage("Số lượng bị từ chối không được âm.");

			RuleFor(x => x.Note)
				.MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");

			RuleForEach(x => x.Batches).SetValidator(new CreateBatchValidator());
		}
	}
}
