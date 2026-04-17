using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.StockAdjustments.Details
{
	public class CreateStockAdjustmentDetailValidator : AbstractValidator<CreateStockAdjustmentDetailRequest>
	{
		public CreateStockAdjustmentDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty()
                .WithMessage("Variant ID là bắt buộc.");

			RuleFor(x => x.BatchId)
				.NotEmpty()
              .WithMessage("Batch ID là bắt buộc.");

			RuleFor(x => x.AdjustmentQuantity)
				.NotEqual(0)
                .WithMessage("Số lượng điều chỉnh không được bằng 0.");
		}
	}
}
