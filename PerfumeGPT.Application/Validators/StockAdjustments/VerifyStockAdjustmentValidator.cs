using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.StockAdjustments
{
	public class VerifyStockAdjustmentValidator : AbstractValidator<VerifyStockAdjustmentRequest>
	{
		public VerifyStockAdjustmentValidator()
		{
			RuleFor(x => x.AdjustmentDetails)
				.NotEmpty()
				.WithMessage("At least one adjustment detail is required.");

			RuleForEach(x => x.AdjustmentDetails)
				.SetValidator(new VerifyStockAdjustmentDetailValidator());
		}
	}

	public class VerifyStockAdjustmentDetailValidator : AbstractValidator<VerifyStockAdjustmentDetailRequest>
	{
		public VerifyStockAdjustmentDetailValidator()
		{
			RuleFor(x => x.DetailId)
				.NotEmpty()
				.WithMessage("Detail ID is required.");

			RuleFor(x => x.ApprovedQuantity)
				.NotEqual(0)
				.WithMessage("Approved quantity cannot be zero.");
		}
	}
}
