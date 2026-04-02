using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Profiles
{
	public record UpdateProfileRequest
	{
		public DateTime? DateOfBirth { get; init; }
		public decimal? MinBudget { get; init; }
		public decimal? MaxBudget { get; init; }

		public List<UpdateNotePreferenceRequest>? NotePreferenceIds { get; init; }
		public List<int>? FamilyPreferenceIds { get; init; }
		public List<int>? AttributePreferenceIds { get; init; }
	}

	public record UpdateNotePreferenceRequest
	{
		public int NoteId { get; init; }
		public NoteType NoteType { get; init; }
	}
}
