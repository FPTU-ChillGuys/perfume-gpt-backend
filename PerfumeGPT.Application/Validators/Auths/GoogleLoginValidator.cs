using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Auths;

namespace PerfumeGPT.Application.Validators.Auths
{
	public class GoogleLoginValidator : AbstractValidator<GoogleLoginRequest>
	{
		public GoogleLoginValidator()
		{
			RuleFor(x => x.IdToken)
				.NotEmpty().WithMessage("Id token là bắt buộc.")
				.MaximumLength(2048).WithMessage("Id token không được vượt quá 2048 ký tự.");
		}
	}
}
