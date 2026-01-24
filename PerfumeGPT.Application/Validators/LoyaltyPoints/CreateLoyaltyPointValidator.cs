using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;

namespace PerfumeGPT.Application.Validators.LoyaltyPoints
{
	public class CreateLoyaltyPointValidator : AbstractValidator<CreateLoyaltyPointRequest>
	{
		public CreateLoyaltyPointValidator()
		{
			RuleFor(x => x.UserId)
				.NotEmpty().WithMessage("UserId is required.")
				.Must(userId => userId != Guid.Empty).WithMessage("UserId cannot be an empty GUID.");
		}
	}
}
