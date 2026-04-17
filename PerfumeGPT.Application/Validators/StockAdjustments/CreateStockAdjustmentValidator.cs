using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.Validators.StockAdjustments.Details;

namespace PerfumeGPT.Application.Validators.StockAdjustments
{
	public class CreateStockAdjustmentValidator : AbstractValidator<CreateStockAdjustmentRequest>
	{
		public CreateStockAdjustmentValidator()
		{
			RuleFor(x => x.AdjustmentDate)
				.NotEmpty()
			   .WithMessage("Ngày điều chỉnh là bắt buộc.");

			RuleFor(x => x.Reason)
				.IsInEnum()
			 .WithMessage("Lý do điều chỉnh không hợp lệ.");

			RuleFor(x => x.AdjustmentDetails)
				.NotEmpty()
			 .WithMessage("Bắt buộc có ít nhất một chi tiết điều chỉnh.")
				.Must(details => details.Count > 0)
				.WithMessage("Bắt buộc có ít nhất một chi tiết điều chỉnh.");

			RuleForEach(x => x.AdjustmentDetails)
				.SetValidator(new CreateStockAdjustmentDetailValidator());
		}
	}
}
