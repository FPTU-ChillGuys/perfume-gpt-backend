using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;

namespace PerfumeGPT.Application.Validators.OrderReturnRequests
{
	public class CreateReturnRequestDtoValidator : AbstractValidator<CreateReturnRequestDto>
	{
		public CreateReturnRequestDtoValidator()
		{
			RuleFor(x => x.OrderId)
				.NotEmpty().WithMessage("ID đơn hàng là bắt buộc.");

			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Ghi chú của khách hàng không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.RefundBankName)
				.MaximumLength(255).WithMessage("Tên ngân hàng hoàn tiền không được vượt quá 255 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
				.MaximumLength(50).WithMessage("Số tài khoản hoàn tiền không được vượt quá 50 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
				.MaximumLength(255).WithMessage("Tên chủ tài khoản hoàn tiền không được vượt quá 255 ký tự.")
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
				.WithMessage("Thông tin ngân hàng không đầy đủ. Tất cả các trường đều bắt buộc nếu yêu cầu hoàn tiền thủ công.");

			RuleFor(x => x.ReturnItems)
				.NotEmpty().WithMessage("Ít nhất một mục trả hàng là bắt buộc.");

			RuleForEach(x => x.ReturnItems)
				.ChildRules(item =>
				{
					item.RuleFor(i => i.OrderDetailId)
						.NotEmpty().WithMessage("ID chi tiết đơn hàng là bắt buộc.");

					item.RuleFor(i => i.Quantity)
						.GreaterThan(0).WithMessage("Số lượng trả hàng phải lớn hơn 0.");
				});

			RuleFor(x => x.ReturnItems)
				.Must(items => items == null || items.GroupBy(i => i.OrderDetailId).All(g => g.Count() == 1))
				.WithMessage("Các mục trả hàng không được chứa ID chi tiết đơn hàng trùng lặp.");

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Các ID media tạm thời phải là duy nhất.");
		}
	}

	public class ProcessInitialReturnDtoValidator : AbstractValidator<ProcessInitialReturnDto>
	{
		public ProcessInitialReturnDtoValidator()
		{
			RuleFor(x => x)
				 .Must(x => !(x.IsApproved && x.IsRequestMoreInfo))
				 .WithMessage("Một yêu cầu hoàn trả không thể vừa được chấp thuận vừa yêu cầu thêm thông tin.");

			RuleFor(x => x.StaffNote)
			  .NotEmpty().WithMessage("Ghi chú của nhân viên là bắt buộc khi từ chối hoặc yêu cầu thêm thông tin.")
				.When(x => !x.IsApproved || x.IsRequestMoreInfo);

			RuleFor(x => x.StaffNote)
				.MaximumLength(1000).WithMessage("Ghi chú của nhân viên không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.StaffNote));
		}
	}

	public class UpdateReturnRequestDtoValidator : AbstractValidator<UpdateReturnRequestDto>
	{
		public UpdateReturnRequestDtoValidator()
		{
			RuleFor(x => x.CustomerNote)
				.MaximumLength(1000).WithMessage("Ghi chú của khách hàng không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.CustomerNote));

			RuleFor(x => x.RefundBankName)
				.MaximumLength(255).WithMessage("Tên ngân hàng hoàn tiền không được vượt quá 255 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
				.MaximumLength(50).WithMessage("Số tài khoản hoàn tiền không được vượt quá 50 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
				.MaximumLength(255).WithMessage("Tên chủ tài khoản hoàn tiền không được vượt quá 255 ký tự.")
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
				.WithMessage("Thông tin ngân hàng không đầy đủ. Tất cả các trường đều bắt buộc nếu yêu cầu hoàn tiền thủ công.");

			RuleFor(x => x.TemporaryMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Các ID media tạm thời phải là duy nhất.");
			RuleFor(x => x.RemoveMediaIds)
				.Must(mediaIds => mediaIds == null || mediaIds.Distinct().Count() == mediaIds.Count)
				.WithMessage("Các ID media cần xóa phải là duy nhất.");
		}
	}

	public class StartInspectionDtoValidator : AbstractValidator<StartInspectionDto>
	{
		public StartInspectionDtoValidator()
		{
			RuleFor(x => x.InspectionNote)
				.MaximumLength(1000).WithMessage("Ghi chú kiểm tra không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.InspectionNote));
		}
	}

	public class RecordInspectionDtoValidator : AbstractValidator<RecordInspectionDto>
	{
		public RecordInspectionDtoValidator()
		{
			RuleFor(x => x.ApprovedRefundAmount)
				.GreaterThanOrEqualTo(0).WithMessage("Số tiền hoàn trả được chấp thuận phải lớn hơn hoặc bằng 0.");

			RuleFor(x => x.InspectionNote)
				.MaximumLength(1000).WithMessage("Ghi chú chi tiết kiểm tra không được vượt quá 1000 ký tự.")
			 .When(x => !string.IsNullOrWhiteSpace(x.InspectionNote));
		}
	}

	public class RejectInspectionDtoValidator : AbstractValidator<RejectInspectionDto>
	{
		public RejectInspectionDtoValidator()
		{
			RuleFor(x => x.Note)
				.NotEmpty().WithMessage("Ghi chú từ chối là bắt buộc.")
				.MaximumLength(1000).WithMessage("Ghi chú từ chối không được vượt quá 1000 ký tự.");
		}
	}
}
