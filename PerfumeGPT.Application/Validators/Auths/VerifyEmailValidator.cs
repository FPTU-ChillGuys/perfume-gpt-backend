using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
	{
		public VerifyEmailValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email is required.")
				.EmailAddress().WithMessage("Invalid email format.");

			RuleFor(x => x.Token)
				.NotEmpty().WithMessage("Token is required.");
		}
	}
}
