using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.StockAdjustments.Details
{
	public class VerifyStockAdjustmentDetailValidator : AbstractValidator<VerifyStockAdjustmentDetailRequest>
	{
		public VerifyStockAdjustmentDetailValidator()
		{
			RuleFor(x => x.DetailId)
				.NotEmpty()
             .WithMessage("Detail ID là bắt buộc.");

			RuleFor(x => x.ApprovedQuantity)
				.NotEqual(0)
              .WithMessage("Số lượng duyệt không được bằng 0.");
		}
	}
}
