namespace PerfumeGPT.Application.DTOs.Requests.Profiles
{
	public class UpdateProfileRequest
	{
		public DateTime? DateOfBirth { get; set; }
		public decimal? MinBudget { get; set; }
		public decimal? MaxBudget { get; set; }

		public List<int> NotePreferenceIds { get; set; } = [];
		public List<int> FamilyPreferenceIds { get; set; } = [];
		public List<int> AttributePreferenceIds { get; set; } = [];
	}
}
