using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;

namespace PerfumeGPT.Application.Validators.Metadatas.Categories
{
	public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
	{
		public UpdateCategoryValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên danh mục không được vượt quá 100 ký tự.");
		}
	}
}
