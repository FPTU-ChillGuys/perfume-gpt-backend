using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Users;

namespace PerfumeGPT.Application.Validators.Users
{
	public class UpdateUserBasicInfoValidator : AbstractValidator<UpdateUserBasicInfoRequest>
	{
		public UpdateUserBasicInfoValidator()
		{
			RuleFor(x => x.FullName)
				.NotEmpty().WithMessage("Full name is required.")
				.MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

			RuleFor(x => x.PhoneNumber)
				.NotEmpty().WithMessage("Phone number is required.")
				.Matches("^[0-9+]{8,15}$").WithMessage("Phone number format is invalid.");
		}
	}
}
