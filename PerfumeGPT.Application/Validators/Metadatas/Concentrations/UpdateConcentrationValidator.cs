using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;

namespace PerfumeGPT.Application.Validators.Metadatas.Concentrations
{
	public class UpdateConcentrationValidator : AbstractValidator<UpdateConcentrationRequest>
	{
		public UpdateConcentrationValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Tên nồng độ là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên nồng độ không được vượt quá 100 ký tự.");
		}
	}
}
