using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Variants;

namespace PerfumeGPT.Application.Validators.Variants
{
	public class UpdateVariantValidator : AbstractValidator<UpdateVariantRequest>
	{
		public UpdateVariantValidator()
		{
			RuleFor(x => x.Sku)
			 .NotEmpty().WithMessage("SKU là bắt buộc.")
				.MaximumLength(50).WithMessage("SKU không được vượt quá 50 ký tự.");
			RuleFor(x => x.VolumeMl)
			 .GreaterThan(0).WithMessage("Thể tích (ml) phải lớn hơn 0.");
			RuleFor(x => x.ConcentrationId)
			 .GreaterThan(0).WithMessage("ConcentrationId phải là số nguyên dương.");
			RuleFor(x => x.BasePrice)
			  .GreaterThanOrEqualTo(0).WithMessage("BasePrice phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.RestockPolicy)
			   .IsInEnum().WithMessage("RestockPolicy không hợp lệ.");
		}
	}
}
