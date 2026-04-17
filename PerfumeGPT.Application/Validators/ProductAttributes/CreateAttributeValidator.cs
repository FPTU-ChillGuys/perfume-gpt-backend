using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Validators.Attributes
{
	public class CreateAttributeValidator : AbstractValidator<CreateAttributeRequest>
	{
		public CreateAttributeValidator()
		{
			RuleFor(x => x.Name)
             .NotEmpty().WithMessage("Tên là bắt buộc")
				.MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự");

			RuleFor(x => x.Description)
              .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự");

			RuleFor(x => x.IsVariantLevel)
               .NotNull().WithMessage("IsVariantLevel là bắt buộc");
		}
	}
}
