using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.Validators.Orders
{
	public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
	{
		public CreateOrderValidator()
		{
			RuleFor(x => x.VoucherId)
				.Must(id => id == null || id != Guid.Empty)
				.WithMessage("VoucherId must be a valid GUID or null.");
		}
	}
}
