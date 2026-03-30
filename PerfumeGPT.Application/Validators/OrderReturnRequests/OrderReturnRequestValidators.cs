using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;

namespace PerfumeGPT.Application.Validators.OrderReturnRequests
{
	public class CreateReturnRequestDtoValidator : AbstractValidator<CreateReturnRequestDto>
	{
		public CreateReturnRequestDtoValidator()
		{
			RuleFor(x => x.OrderId)
				.NotEmpty().WithMessage("Order ID is required.");

			RuleFor(x => x.Reason)
				.NotEmpty().WithMessage("Reason is required.")
				.MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");

			RuleFor(x => x.RequestedRefundAmount)
				.GreaterThan(0).WithMessage("Requested refund amount must be greater than 0.");

			RuleFor(x => x.ReturnItems)
				.NotEmpty().WithMessage("At least one return item is required.");

			RuleForEach(x => x.ReturnItems)
				.SetValidator(new CreateReturnRequestDetailDtoValidator());

			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Customer note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Temporary media IDs must be unique.");
		}
	}

	public class CreateReturnRequestDetailDtoValidator : AbstractValidator<CreateReturnRequestDetailDto>
	{
		public CreateReturnRequestDetailDtoValidator()
		{
			RuleFor(x => x.OrderDetailId)
				.NotEmpty().WithMessage("Order detail ID is required.");

			RuleFor(x => x.ReturnedQuantity)
				.GreaterThan(0).WithMessage("Returned quantity must be greater than 0.");
		}
	}

	public class ProcessInitialReturnDtoValidator : AbstractValidator<ProcessInitialReturnDto>
	{
		public ProcessInitialReturnDtoValidator()
		{
			RuleFor(x => x.StaffNote)
				.NotEmpty().WithMessage("Staff note is required when rejecting a return request.")
				.When(x => !x.IsApproved);

			RuleFor(x => x.StaffNote)
				.MaximumLength(1000).WithMessage("Staff note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.StaffNote));
		}
	}

	public class StartInspectionDtoValidator : AbstractValidator<StartInspectionDto>
	{
		public StartInspectionDtoValidator()
		{
			RuleFor(x => x.InspectionNote)
				.MaximumLength(1000).WithMessage("Inspection note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.InspectionNote));
		}
	}

	public class RecordInspectionDtoValidator : AbstractValidator<RecordInspectionDto>
	{
		public RecordInspectionDtoValidator()
		{
			RuleFor(x => x.ApprovedRefundAmount)
				.GreaterThanOrEqualTo(0).WithMessage("Approved refund amount must be greater than or equal to 0.");

			RuleFor(x => x.InspectionResults)
				.NotEmpty().WithMessage("Inspection results are required.");

			RuleForEach(x => x.InspectionResults)
				.SetValidator(new RecordInspectionDetailDtoValidator());
		}
	}

	public class RecordInspectionDetailDtoValidator : AbstractValidator<RecordInspectionDetailDto>
	{
		public RecordInspectionDetailDtoValidator()
		{
			RuleFor(x => x.DetailId)
				.NotEmpty().WithMessage("Return detail ID is required.");

			RuleFor(x => x.Note)
				.MaximumLength(1000).WithMessage("Inspection detail note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.Note));
		}
	}

	public class RejectInspectionDtoValidator : AbstractValidator<RejectInspectionDto>
	{
		public RejectInspectionDtoValidator()
		{
			RuleFor(x => x.Note)
				.NotEmpty().WithMessage("Rejection note is required.")
				.MaximumLength(1000).WithMessage("Rejection note must not exceed 1000 characters.");
		}
	}
}
