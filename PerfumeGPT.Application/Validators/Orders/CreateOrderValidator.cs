using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.Validators.Orders
{
	public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
	{
		public CreateOrderValidator()
		{
			RuleFor(x => x.CustomerId)
				.Must(id => id != Guid.Empty)
				.WithMessage("CustomerId must be a valid GUID or null.");
			RuleFor(x => x.StaffId)
				.NotEmpty().WithMessage("StaffId is required.")
				.Must(id => id != Guid.Empty)
				.WithMessage("StaffId must be a valid GUID.");
			RuleFor(x => x.VoucherId)
				.Must(id => id == null || id != Guid.Empty)
				.WithMessage("VoucherId must be a valid GUID or null.");
		}
	}
}
