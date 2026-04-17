using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;

namespace PerfumeGPT.Application.Validators.SourcingCatalogs
{
	public class CreateCatalogItemValidator : AbstractValidator<CreateCatalogItemRequest>
	{
		public CreateCatalogItemValidator()
		{
			RuleFor(x => x.ProductVariantId)
                .NotEmpty().WithMessage("ProductVariantId là bắt buộc.")
				.NotEqual(Guid.Empty).WithMessage("ProductVariantId phải là GUID hợp lệ.");

			RuleFor(x => x.SupplierId)
              .GreaterThan(0).WithMessage("SupplierId phải là số nguyên dương.");

			RuleFor(x => x.NegotiatedPrice)
             .GreaterThan(0).WithMessage("Giá thương lượng phải lớn hơn 0.");

			RuleFor(x => x.EstimatedLeadTimeDays)
              .GreaterThanOrEqualTo(0).WithMessage("Số ngày giao hàng dự kiến không được âm.");
		}
	}
}
