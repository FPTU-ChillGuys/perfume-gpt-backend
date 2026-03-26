using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;

namespace PerfumeGPT.Application.Validators.Metadatas.ScentNotes
{
	public class CreateScentNoteValidator : AbstractValidator<CreateScentNoteRequest>
	{
		public CreateScentNoteValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Scent note name is required.")
				.MaximumLength(100).WithMessage("Scent note name cannot exceed 100 characters.");
		}
	}
}
