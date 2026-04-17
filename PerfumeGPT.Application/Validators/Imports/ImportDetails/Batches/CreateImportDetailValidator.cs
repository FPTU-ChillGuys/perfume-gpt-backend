using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches
{
	public class CreateImportDetailValidator : AbstractValidator<CreateImportDetailRequest>
	{
		public CreateImportDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID là bắt buộc.");

			RuleFor(x => x.ExpectedQuantity)
				.GreaterThan(0).WithMessage("Số lượng dự kiến phải lớn hơn 0.");

			RuleFor(x => x.UnitPrice)
				.GreaterThan(0).WithMessage("Đơn giá phải lớn hơn 0.");
		}
	}
}
