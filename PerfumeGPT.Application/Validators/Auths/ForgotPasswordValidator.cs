using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
	{
		public ForgotPasswordValidator()
		{
			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("Email is required.")
				.EmailAddress().WithMessage("Invalid email format.")
				.MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

			RuleFor(x => x.ClientUri)
				.NotEmpty().WithMessage("Client URI is required.")
				.MaximumLength(2048).WithMessage("Client URI must not exceed 2048 characters.");
		}
	}
}
