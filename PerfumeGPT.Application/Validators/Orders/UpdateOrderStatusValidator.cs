using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Validators.Orders
{
	public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
	{
		public UpdateOrderStatusValidator()
		{
			RuleFor(x => x.Status)
				.IsInEnum()
				.WithMessage("Invalid order status.");

			RuleFor(x => x.Note)
				.MaximumLength(500)
				.When(x => !string.IsNullOrEmpty(x.Note))
				.WithMessage("Note cannot exceed 500 characters.");
		}
	}
}
