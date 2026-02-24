using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class RegisterValidator : AbstractValidator<RegisterRequest>
	{
		public RegisterValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email is required.")
				.EmailAddress().WithMessage("Invalid email format.");
			RuleFor(x => x.PhoneNumber)
				.NotEmpty().WithMessage("Phone number is required.")
				.Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format.");
		}
	}
}
