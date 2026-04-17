using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
	{
		public VerifyEmailValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email là bắt buộc.")
				.EmailAddress().WithMessage("Định dạng email không hợp lệ.");

			RuleFor(x => x.Token)
				.NotEmpty().WithMessage("Token là bắt buộc.");
		}
	}
}
