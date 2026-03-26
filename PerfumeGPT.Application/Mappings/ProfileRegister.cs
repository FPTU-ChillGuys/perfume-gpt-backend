using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ProfileRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CustomerProfile, ProfileResponse>()
				.Map(dest => dest.MinBudget, src => src.MinBudget)
				.Map(dest => dest.MaxBudget, src => src.MaxBudget)
				.Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
				.Map(dest => dest.NotePreferences, src => src.NotePreferences.Select(np => new CustomerNotePreferenceResponse
				{
					NoteId = np.NoteId,
					NoteName = np.ScentNote.Name,
					NoteType = np.NoteType
				}).ToList())
				.Map(dest => dest.FamilyPreferences, src => src.FamilyPreferences.Select(bp => new CustomerFamilyPreferenceRespone
				{
					FamilyId = bp.FamilyId,
					FamilyName = bp.Family.Name
				}).ToList())
				.Map(dest => dest.AttributePreferences, src => src.AttributePreferences.Select(ap => new CustomerAttributePreferenceResponse
				{
					AttributeValueId = ap.AttributeValueId,
					AttributeValueName = ap.AttributeValue.Value
				}).ToList());
		}
	}
}
