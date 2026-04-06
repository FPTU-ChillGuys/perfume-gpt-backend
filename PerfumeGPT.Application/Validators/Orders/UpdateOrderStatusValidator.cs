using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.Validators.Orders
{
	public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
	{
		public UpdateOrderStatusValidator()
		{
			RuleFor(x => x.Note)
				.MaximumLength(500)
				.When(x => !string.IsNullOrEmpty(x.Note))
				.WithMessage("Note cannot exceed 500 characters.");
		}
	}
}
