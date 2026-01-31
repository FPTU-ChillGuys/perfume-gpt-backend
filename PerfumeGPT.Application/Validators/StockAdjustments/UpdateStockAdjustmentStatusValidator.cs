using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;

namespace PerfumeGPT.Application.Validators.StockAdjustments
{
	public class UpdateStockAdjustmentStatusValidator : AbstractValidator<UpdateStockAdjustmentStatusRequest>
	{
		public UpdateStockAdjustmentStatusValidator()
		{
			RuleFor(x => x.Status)
				.IsInEnum()
				.WithMessage("Invalid adjustment status.");
		}
	}
}
