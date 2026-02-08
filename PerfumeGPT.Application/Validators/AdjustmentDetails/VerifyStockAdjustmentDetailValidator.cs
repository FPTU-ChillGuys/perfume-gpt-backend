using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.AdjustmentDetails
{
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
