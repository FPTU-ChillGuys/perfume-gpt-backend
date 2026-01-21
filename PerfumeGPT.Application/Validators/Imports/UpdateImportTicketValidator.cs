using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;

namespace PerfumeGPT.Application.Validators.Imports
{
	public class UpdateImportTicketValidator : AbstractValidator<UpdateImportTicketRequest>
	{
		public UpdateImportTicketValidator()
		{
			RuleFor(x => x.Status)
				.IsInEnum().WithMessage("Invalid import status.");
		}
	}
}
