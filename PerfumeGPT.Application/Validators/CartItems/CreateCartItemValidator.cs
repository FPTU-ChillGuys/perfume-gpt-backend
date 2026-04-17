using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Carts;

namespace PerfumeGPT.Application.Validators.CartItems
{
	public class CreateCartItemValidator : AbstractValidator<CreateCartItemRequest>
	{
		public CreateCartItemValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("VariantId là bắt buộc.")
				.Must(variantId => variantId != Guid.Empty).WithMessage("VariantId không được là GUID rỗng.");
			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
		}
	}
}
