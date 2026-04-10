using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ProfileRepository : GenericRepository<CustomerProfile>, IProfileRepository
	{
		public ProfileRepository(PerfumeDbContext context) : base(context) { }

		public async Task<CustomerProfile?> GetByUserIdWithPreferencesAsync(Guid userId)
		=> await _context.CustomerProfiles
			.Include(p => p.NotePreferences)
			.Include(p => p.FamilyPreferences)
			.Include(p => p.AttributePreferences)
			.FirstOrDefaultAsync(p => p.UserId == userId);

		public async Task<ProfileResponse?> GetProfileResponseByUserIdAsync(Guid userId)
		=> await _context.CustomerProfiles
			.Where(p => p.UserId == userId)
			.Select(p => new ProfileResponse
			{
				DateOfBirth = p.DateOfBirth,
				MinBudget = p.MinBudget,
				MaxBudget = p.MaxBudget,
				NotePreferences = p.NotePreferences
					.Select(np => new CustomerNotePreferenceResponse
					{
						NoteId = np.NoteId,
						NoteName = np.ScentNote.Name,
						NoteType = np.NoteType
					})
					.ToList(),
				FamilyPreferences = p.FamilyPreferences
					.Select(fp => new CustomerFamilyPreferenceRespone
					{
						FamilyId = fp.FamilyId,
						FamilyName = fp.Family.Name
					})
					.ToList(),
				AttributePreferences = p.AttributePreferences
					.Select(ap => new CustomerAttributePreferenceResponse
					{
						AttributeValueId = ap.AttributeValueId,
						AttributeValueName = ap.AttributeValue.Value
					})
					.ToList()
			})
			.FirstOrDefaultAsync();

		public async Task<List<int>> GetMissingNoteIdsAsync(IEnumerable<int> noteIds)
		{
			var ids = noteIds?.Distinct().ToList() ?? [];
			if (ids.Count == 0) return [];

			var existingIds = await _context.ScentNotes
				.Where(x => ids.Contains(x.Id))
				.Select(x => x.Id)
				.ToListAsync();

			return [.. ids.Except(existingIds)];
		}

		public async Task<List<int>> GetMissingFamilyIdsAsync(IEnumerable<int> familyIds)
		{
			var ids = familyIds?.Distinct().ToList() ?? [];
			if (ids.Count == 0) return [];

			var existingIds = await _context.OlfactoryFamilies
				.Where(x => ids.Contains(x.Id))
				.Select(x => x.Id)
				.ToListAsync();

			return [.. ids.Except(existingIds)];
		}

		public async Task<List<int>> GetMissingAttributeValueIdsAsync(IEnumerable<int> attributeValueIds)
		{
			var ids = attributeValueIds?.Distinct().ToList() ?? [];
			if (ids.Count == 0) return [];

			var existingIds = await _context.AttributeValues
				.Where(x => ids.Contains(x.Id))
				.Select(x => x.Id)
				.ToListAsync();

			return [.. ids.Except(existingIds)];
		}
	}
}
