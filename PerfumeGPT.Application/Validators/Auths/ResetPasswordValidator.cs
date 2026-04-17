using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
	{
		public ResetPasswordValidator()
		{
			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Mật khẩu mới là bắt buộc");

			RuleFor(x => x.ConfirmPassword)
				.NotEmpty().WithMessage("Xác nhận mật khẩu là bắt buộc")
				.Equal(x => x.Password).WithMessage("Xác nhận mật khẩu phải khớp với mật khẩu");
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email là bắt buộc")
				.EmailAddress().WithMessage("Định dạng email không hợp lệ");

			RuleFor(x => x.Token)
				.NotEmpty().WithMessage("Token là bắt buộc");
		}
	}
}
