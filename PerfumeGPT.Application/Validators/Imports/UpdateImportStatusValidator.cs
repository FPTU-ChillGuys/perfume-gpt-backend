using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class UpdateImportStatusValidator : AbstractValidator<UpdateImportStatusRequest>
	{
		public UpdateImportStatusValidator()
		{
			RuleFor(x => x.Status)
				.IsInEnum().WithMessage("Invalid import status.");
		}
	}
}
