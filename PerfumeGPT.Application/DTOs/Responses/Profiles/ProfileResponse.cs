using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Profiles
{
	public record ProfileResponse
	{
		public DateTime? DateOfBirth { get; init; }
		public decimal? MinBudget { get; init; }
		public decimal? MaxBudget { get; init; }

		public required List<CustomerNotePreferenceResponse> NotePreferences { get; init; }
		public required List<CustomerFamilyPreferenceRespone> FamilyPreferences { get; init; }
		public required List<CustomerAttributePreferenceResponse> AttributePreferences { get; init; }
	}

	public record CustomerNotePreferenceResponse
	{
		public int NoteId { get; init; }
		public required string NoteName { get; init; }
		public NoteType NoteType { get; init; }
	}

	public record CustomerFamilyPreferenceRespone
	{
		public int FamilyId { get; init; }
		public required string FamilyName { get; init; }
	}

	public record CustomerAttributePreferenceResponse
	{
		public int AttributeValueId { get; init; }
		public required string AttributeValueName { get; init; }
	}
}
