using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;

namespace PerfumeGPT.Application.Validators.CartItems
{
	public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
	{
		public UpdateCartItemValidator()
		{
			RuleFor(x => x.Quantity)
				.GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to 0.");
		}
	}
}
