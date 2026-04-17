using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.Validators.StockAdjustments.Details;

namespace PerfumeGPT.Application.Validators.StockAdjustments
{
	public class VerifyStockAdjustmentValidator : AbstractValidator<VerifyStockAdjustmentRequest>
	{
		public VerifyStockAdjustmentValidator()
		{
			RuleFor(x => x.AdjustmentDetails)
				.NotEmpty()
				.WithMessage("Bắt buộc có ít nhất một chi tiết điều chỉnh.");

			RuleForEach(x => x.AdjustmentDetails)
				.SetValidator(new VerifyStockAdjustmentDetailValidator());
		}
	}
}
