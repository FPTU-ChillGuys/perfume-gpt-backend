namespace PerfumeGPT.Application.DTOs.Responses.Profiles
{
	public class ProfileResponse
	{
		public DateTime? DateOfBirth { get; set; }
		public decimal? MinBudget { get; set; }
		public decimal? MaxBudget { get; set; }

		public List<CustomerNotePreferenceResponse> NotePreferences { get; set; } = [];
		public List<CustomerFamilyPreferenceRespone> FamilyPreferences { get; set; } = [];
		public List<CustomerAttributePreferenceResponse> AttributePreferences { get; set; } = [];
	}

	public class CustomerNotePreferenceResponse
	{
		public int NoteId { get; set; }
		public string NoteName { get; set; } = string.Empty;
		public int PreferenceLevel { get; set; }
	}

	public class CustomerFamilyPreferenceRespone
	{
		public int FamilyId { get; set; }
		public string FamilyName { get; set; } = string.Empty;
	}

	public class CustomerAttributePreferenceResponse
	{
		public int AttributeValueId { get; set; }
		public string AttributeValueName { get; set; } = string.Empty;
	}
}
