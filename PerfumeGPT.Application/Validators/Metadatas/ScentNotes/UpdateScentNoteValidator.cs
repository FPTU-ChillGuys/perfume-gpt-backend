using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;

namespace PerfumeGPT.Application.Validators.Metadatas.ScentNotes
{
	public class UpdateScentNoteValidator : AbstractValidator<UpdateScentNoteRequest>
	{
		public UpdateScentNoteValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Name is required.")
				.MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
		}
	}
}
