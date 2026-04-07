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

			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Customer note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.RefundBankName)
				.MaximumLength(255).WithMessage("Refund bank name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
				.MaximumLength(50).WithMessage("Refund account number must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
				.MaximumLength(255).WithMessage("Refund account name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountName));

			RuleFor(x => x)
				.Must(x =>
				{
					var hasBankInfo = !string.IsNullOrWhiteSpace(x.RefundBankName)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountName);

					if (!hasBankInfo)
						return true;

					return !string.IsNullOrWhiteSpace(x.RefundBankName)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountName);
				})
				.WithMessage("Incomplete bank information. All fields are required if requesting a manual refund.");

			RuleFor(x => x.ReturnItems)
				.NotEmpty().WithMessage("At least one return item is required.");

			RuleForEach(x => x.ReturnItems)
				.ChildRules(item =>
				{
					item.RuleFor(i => i.OrderDetailId)
						.NotEmpty().WithMessage("Order detail ID is required.");

					item.RuleFor(i => i.Quantity)
						.GreaterThan(0).WithMessage("Return quantity must be greater than 0.");
				});

			RuleFor(x => x.ReturnItems)
				.Must(items => items == null || items.GroupBy(i => i.OrderDetailId).All(g => g.Count() == 1))
				.WithMessage("Return items must not contain duplicate order detail IDs.");

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Temporary media IDs must be unique.");
		}
	}

	public class ProcessInitialReturnDtoValidator : AbstractValidator<ProcessInitialReturnDto>
	{
		public ProcessInitialReturnDtoValidator()
		{
			RuleFor(x => x)
				 .Must(x => !(x.IsApproved && x.IsRequestMoreInfo))
				 .WithMessage("A return request cannot be both approved and require more info.");

			RuleFor(x => x.StaffNote)
			  .NotEmpty().WithMessage("Staff note is required when rejecting or requesting more info.")
				.When(x => !x.IsApproved || x.IsRequestMoreInfo);

			RuleFor(x => x.StaffNote)
				.MaximumLength(1000).WithMessage("Staff note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.StaffNote));
		}
	}

	public class UpdateReturnRequestDtoValidator : AbstractValidator<UpdateReturnRequestDto>
	{
		public UpdateReturnRequestDtoValidator()
		{
			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Customer note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.RefundBankName)
				.MaximumLength(255).WithMessage("Refund bank name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
				.MaximumLength(50).WithMessage("Refund account number must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
				.MaximumLength(255).WithMessage("Refund account name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountName));

			RuleFor(x => x)
				.Must(x =>
				{
					var hasBankInfo = !string.IsNullOrWhiteSpace(x.RefundBankName)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountName);

					if (!hasBankInfo)
						return true;

					return !string.IsNullOrWhiteSpace(x.RefundBankName)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountName);
				})
				.WithMessage("Incomplete bank information. All fields are required if requesting a manual refund.");

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Temporary media IDs must be unique.");

			RuleFor(x => x.RemoveMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Remove media IDs must be unique.");
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
