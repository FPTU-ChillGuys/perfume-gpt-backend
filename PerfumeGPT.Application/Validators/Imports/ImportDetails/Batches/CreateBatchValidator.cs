using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;

namespace PerfumeGPT.Application.Validators.Imports.ImportDetails.Batches
{
	public class CreateBatchValidator : AbstractValidator<CreateBatchRequest>
	{
		public CreateBatchValidator()
		{
			RuleFor(x => x.BatchCode)
				.NotEmpty().WithMessage("Mã lô là bắt buộc.")
				.MaximumLength(50).WithMessage("Mã lô không được vượt quá 50 ký tự.");

			RuleFor(x => x.ManufactureDate)
				.NotEmpty().WithMessage("Ngày sản xuất là bắt buộc.")
				.LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Ngày sản xuất không được ở tương lai.");
			RuleFor(x => x.ExpiryDate)
				.NotEmpty().WithMessage("Ngày hết hạn là bắt buộc.")
				.GreaterThan(x => x.ManufactureDate).WithMessage("Ngày hết hạn phải sau ngày sản xuất.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Số lượng lô phải lớn hơn 0.");
		}
	}
}
