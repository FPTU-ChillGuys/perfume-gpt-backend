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
				.NotEmpty().WithMessage("Credential is required.")
				.Must(credential => IsValidEmail(credential) || IsValidPhoneNumber(credential))
				.WithMessage("Credential must be a valid email or Vietnamese phone number.");

			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Password is required.");
		}

		private static bool IsValidEmail(string value) =>
			new EmailAddressAttribute().IsValid(value);

		private static bool IsValidPhoneNumber(string value) =>
			Regex.IsMatch(value, @"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$");
	}
}
