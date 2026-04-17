using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Validators.ProductAttributes
{
	public class UpdateAttributeValueValidator : AbstractValidator<UpdateAttributeValueRequest>
	{
		public UpdateAttributeValueValidator()
		{
			RuleFor(x => x.Value)
               .NotEmpty().WithMessage("Giá trị là bắt buộc.")
				.MaximumLength(200).WithMessage("Giá trị không được vượt quá 200 ký tự.");
		}
	}
}
