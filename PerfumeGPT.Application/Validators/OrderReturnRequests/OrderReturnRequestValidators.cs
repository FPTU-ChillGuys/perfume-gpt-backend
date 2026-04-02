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

			RuleFor(x => x.RequestedRefundAmount)
				.GreaterThan(0).WithMessage("Requested refund amount must be greater than 0.");

			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Customer note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Temporary media IDs must be unique.");
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

			RuleFor(x => x.InspectionNote)
				.MaximumLength(1000).WithMessage("Inspection detail note must not exceed 1000 characters.")
			 .When(x => !string.IsNullOrWhiteSpace(x.InspectionNote));
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
