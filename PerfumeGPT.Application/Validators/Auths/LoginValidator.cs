using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class LoginValidator : AbstractValidator<LoginRequest>
	{
		public LoginValidator()
		{
			RuleFor(x => x.Credential)
				.NotEmpty().WithMessage("Thông tin đăng nhập là bắt buộc.")
				.Must(credential => IsValidEmail(credential) || IsValidPhoneNumber(credential))
				.WithMessage("Thông tin đăng nhập phải là email hợp lệ hoặc số điện thoại Việt Nam hợp lệ.");

			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Mật khẩu là bắt buộc.");
		}

		private static bool IsValidEmail(string value) => new EmailAddressAttribute().IsValid(value);

		private static bool IsValidPhoneNumber(string value) =>
			Regex.IsMatch(value, @"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$");
	}
}
