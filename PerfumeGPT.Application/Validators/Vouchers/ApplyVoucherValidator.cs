using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;

namespace PerfumeGPT.Application.Validators.Vouchers
{
	public class ApplyVoucherValidator : AbstractValidator<ApplyVoucherRequest>
	{
		public ApplyVoucherValidator()
		{
			RuleFor(x => x.VoucherId)
				.NotEmpty().WithMessage("Voucher ID is required.");

			RuleFor(x => x.OrderAmount)
				.GreaterThan(0).WithMessage("Order amount must be greater than 0.");
		}
	}
}
