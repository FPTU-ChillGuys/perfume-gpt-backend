using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
	{
		public ResetPasswordValidator()
		{
			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Password is required");

			RuleFor(x => x.ConfirmPassword)
				.NotEmpty().WithMessage("Confirm Password is required")
				.Equal(x => x.Password).WithMessage("Confirm Password must match Password");

			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email is required")
				.EmailAddress().WithMessage("Invalid email format");

			RuleFor(x => x.Token)
				.NotEmpty().WithMessage("Token is required");
		}
	}
}
