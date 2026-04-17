using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class RegisterValidator : AbstractValidator<RegisterRequest>
	{
		public RegisterValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email là bắt buộc.")
				.EmailAddress().WithMessage("Định dạng email không hợp lệ.");
			RuleFor(x => x.PhoneNumber)
				.NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
				.Matches(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$").WithMessage("Định dạng số điện thoại không hợp lệ.");
		}
	}
}
