using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;

namespace PerfumeGPT.Application.Validators.Metadatas.Brands
{
	public class CreateBrandValidator : AbstractValidator<CreateBrandRequest>
	{
		public CreateBrandValidator()
		{
			RuleFor(x => x.Name)
				.Must(name => !string.IsNullOrWhiteSpace(name))
				.WithMessage("Tên thương hiệu không được để trống.")
				.MaximumLength(100)
				.WithMessage("Tên thương hiệu không được vượt quá 100 ký tự.");
		}
	}
}
