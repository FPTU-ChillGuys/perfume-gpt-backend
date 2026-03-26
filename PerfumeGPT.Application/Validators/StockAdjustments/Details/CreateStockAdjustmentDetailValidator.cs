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
				.WithMessage("Variant ID is required.");

			RuleFor(x => x.BatchId)
				.NotEmpty()
				.WithMessage("Batch ID is required.");

			RuleFor(x => x.AdjustmentQuantity)
				.NotEqual(0)
				.WithMessage("Adjustment quantity cannot be zero.");
		}
	}
}
