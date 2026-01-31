using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.StockAdjustments
{
	public class CreateStockAdjustmentValidator : AbstractValidator<CreateStockAdjustmentRequest>
	{
		public CreateStockAdjustmentValidator()
		{
			RuleFor(x => x.AdjustmentDate)
				.NotEmpty()
				.WithMessage("Adjustment date is required.");

			RuleFor(x => x.Reason)
				.IsInEnum()
				.WithMessage("Invalid adjustment reason.");

			RuleFor(x => x.AdjustmentDetails)
				.NotEmpty()
				.WithMessage("At least one adjustment detail is required.")
				.Must(details => details.Count > 0)
				.WithMessage("At least one adjustment detail is required.");

			RuleForEach(x => x.AdjustmentDetails)
				.SetValidator(new CreateStockAdjustmentDetailValidator());
		}
	}

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
