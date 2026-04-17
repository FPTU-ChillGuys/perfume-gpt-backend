using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;

namespace PerfumeGPT.Application.Validators.Metadatas.OlfactoryFamilies
{
	public class UpdateOlfactoryFamilyValidator : AbstractValidator<UpdateOlfactoryFamilyRequest>
	{
		public UpdateOlfactoryFamilyValidator()
		{
			RuleFor(x => x.Name).NotEmpty().WithMessage("Tên nhóm mùi là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên nhóm mùi không được vượt quá 100 ký tự.");
		}
	}
}
