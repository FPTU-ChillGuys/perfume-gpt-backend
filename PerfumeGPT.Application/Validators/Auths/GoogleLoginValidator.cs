using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class GoogleLoginValidator : AbstractValidator<GoogleLoginRequest>
	{
		public GoogleLoginValidator()
		{
			RuleFor(x => x.IdToken)
				.NotEmpty().WithMessage("Id token is required.")
				.MaximumLength(2048).WithMessage("Id token must not exceed 2048 characters.");
		}
	}
}
