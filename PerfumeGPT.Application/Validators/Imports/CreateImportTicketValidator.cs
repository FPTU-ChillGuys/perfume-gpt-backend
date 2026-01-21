using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class CreateImportTicketValidator : AbstractValidator<CreateImportTicketRequest>
	{
		public CreateImportTicketValidator()
		{
			RuleFor(x => x.SupplierId)
				.GreaterThan(0).WithMessage("Supplier ID must be a positive integer.");

			RuleFor(x => x.ImportDate)
				.NotEmpty().WithMessage("Import date is required.")
				.LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Import date cannot be in the future.");

			RuleFor(x => x.ImportDetails)
				.NotEmpty().WithMessage("Import details are required.")
				.Must(details => details != null && details.Count > 0).WithMessage("At least one import detail is required.");

			RuleForEach(x => x.ImportDetails).SetValidator(new CreateImportDetailValidator());
		}
	}

	public class CreateImportDetailValidator : AbstractValidator<CreateImportDetailRequest>
	{
		public CreateImportDetailValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID is required.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than 0.");

			RuleFor(x => x.UnitPrice)
				.GreaterThan(0).WithMessage("Unit price must be greater than 0.");
		}
	}

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
