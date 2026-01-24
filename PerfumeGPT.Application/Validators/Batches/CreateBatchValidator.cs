using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Batches
{
	public class CreateBatchValidator : AbstractValidator<CreateBatchRequest>
	{
		public CreateBatchValidator()
		{
			RuleFor(x => x.BatchCode)
				.NotEmpty().WithMessage("Batch code is required.")
				.MaximumLength(50).WithMessage("Batch code must not exceed 50 characters.");

			RuleFor(x => x.ManufactureDate)
				.NotEmpty().WithMessage("Manufacture date is required.")
				.LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Manufacture date cannot be in the future.");

			RuleFor(x => x.ExpiryDate)
				.NotEmpty().WithMessage("Expiry date is required.")
				.GreaterThan(x => x.ManufactureDate).WithMessage("Expiry date must be after manufacture date.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Batch quantity must be greater than 0.");
		}
	}
}
