using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
	{
		public ForgotPasswordValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email là bắt buộc.")
				.EmailAddress().WithMessage("Định dạng email không hợp lệ.")
				.MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

			RuleFor(x => x.ClientUri)
				.NotEmpty().WithMessage("Client URI là bắt buộc.")
				.MaximumLength(2048).WithMessage("Client URI không được vượt quá 2048 ký tự.");
		}
	}
}
