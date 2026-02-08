using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.Validators.AdjustmentDetails;

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
}
