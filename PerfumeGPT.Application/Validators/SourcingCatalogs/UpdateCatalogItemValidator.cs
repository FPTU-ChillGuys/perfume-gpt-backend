using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;

namespace PerfumeGPT.Application.Validators.SourcingCatalogs
{
	public class UpdateCatalogItemValidator : AbstractValidator<UpdateCatalogItemRequest>
	{
		public UpdateCatalogItemValidator()
		{
			RuleFor(x => x.NegotiatedPrice)
             .GreaterThan(0).WithMessage("Giá thương lượng phải lớn hơn 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
              .GreaterThanOrEqualTo(0).WithMessage("Số ngày giao hàng dự kiến không được âm.");
		}
	}
}
