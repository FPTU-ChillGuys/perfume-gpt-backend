using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;

namespace PerfumeGPT.Application.Validators.Metadatas.ScentNotes
{
	public class CreateScentNoteValidator : AbstractValidator<CreateScentNoteRequest>
	{
		public CreateScentNoteValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Tên nốt hương là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên nốt hương không được vượt quá 100 ký tự.");
		}
	}
}
