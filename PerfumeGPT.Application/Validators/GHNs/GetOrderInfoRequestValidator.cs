using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.GHNs;

namespace PerfumeGPT.Application.Validators.GHNs
{
	public class GetOrderInfoRequestValidator : AbstractValidator<GetOrderInfoRequest>
	{
		public GetOrderInfoRequestValidator()
		{
			RuleFor(x => x.TrackingNumbers)
				.NotNull().WithMessage("Mã vận đơn là bắt buộc.")
				.Must(x => x.Count > 0).WithMessage("Mã vận đơn phải chứa ít nhất một giá trị.");

			RuleForEach(x => x.TrackingNumbers)
				.NotEmpty().WithMessage("Mã vận đơn không được để trống.");
		}
	}
}
