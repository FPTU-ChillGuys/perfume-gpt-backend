using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Carts;

namespace PerfumeGPT.Application.Validators.CartItems
{
	public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
	{
		public UpdateCartItemValidator()
		{
			RuleFor(x => x.Quantity)
				.GreaterThanOrEqualTo(0).WithMessage("Số lượng phải lớn hơn hoặc bằng 0.");
		}
	}
}
