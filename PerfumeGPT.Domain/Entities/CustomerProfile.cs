using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerProfile : BaseEntity<Guid>, IHasTimestamps
	{
		protected CustomerProfile() { }

		public Guid UserId { get; private set; }
		public DateTime? DateOfBirth { get; private set; }
		public decimal? MinBudget { get; private set; }
		public decimal? MaxBudget { get; private set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual ICollection<CustomerNotePreference> NotePreferences { get; set; } = [];
		public virtual ICollection<CustomerFamilyPreference> FamilyPreferences { get; set; } = [];
		public virtual ICollection<CustomerAttributePreference> AttributePreferences { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory method
		public static CustomerProfile Create(Guid userId)
		{
			if (userId == Guid.Empty)
			{
               throw DomainException.BadRequest("User ID là bắt buộc.");
			}

			return new CustomerProfile
			{
				UserId = userId
			};
		}

		public void UpdateBasicInfo(DateTime? dateOfBirth, decimal? minBudget, decimal? maxBudget)
		{
			if (minBudget.HasValue && minBudget.Value < 0)
			{
              throw DomainException.BadRequest("Ngân sách tối thiểu phải lớn hơn hoặc bằng 0.");
			}

			if (maxBudget.HasValue && maxBudget.Value < 0)
			{
              throw DomainException.BadRequest("Ngân sách tối đa phải lớn hơn hoặc bằng 0.");
			}

			if (minBudget.HasValue && maxBudget.HasValue && minBudget.Value > maxBudget.Value)
			{
                throw DomainException.BadRequest("Ngân sách tối thiểu không được lớn hơn ngân sách tối đa.");
			}

			DateOfBirth = dateOfBirth;
			MinBudget = minBudget;
			MaxBudget = maxBudget;
		}

		public void UpdateNotePreferences(IEnumerable<(int NoteId, NoteType NoteType)>? notePreferences)
		{
			var distinctNewPreferences = notePreferences?.Distinct().ToList() ?? [];

			if (distinctNewPreferences.Any(np => np.NoteId <= 0))
                throw DomainException.BadRequest("Tất cả ID sở thích nốt hương phải lớn hơn 0.");

			if (distinctNewPreferences.Any(np => !Enum.IsDefined(np.NoteType)))
               throw DomainException.BadRequest("Tất cả loại sở thích nốt hương phải hợp lệ.");

			var newPreferenceSet = distinctNewPreferences.ToHashSet();

			var itemsToRemove = NotePreferences
			   .Where(np => !newPreferenceSet.Contains((np.NoteId, np.NoteType)))
				.ToList();

			foreach (var item in itemsToRemove)
			{
				NotePreferences.Remove(item);
			}

			var existingPreferences = NotePreferences
				.Select(np => (np.NoteId, np.NoteType))
				.ToHashSet();
			var preferencesToAdd = distinctNewPreferences
				.Where(np => !existingPreferences.Contains(np));

			foreach (var notePreference in preferencesToAdd)
			{
				NotePreferences.Add(CustomerNotePreference.Create(Id, notePreference.NoteId, notePreference.NoteType));
			}
		}

		public void UpdateFamilyPreferences(IEnumerable<int>? familyPreferenceIds)
		{
			var distinctNewIds = familyPreferenceIds?.Distinct().ToList() ?? [];

			if (distinctNewIds.Any(id => id <= 0))
              throw DomainException.BadRequest("Tất cả ID sở thích nhóm hương phải lớn hơn 0.");

			var itemsToRemove = FamilyPreferences
				.Where(fp => !distinctNewIds.Contains(fp.FamilyId))
				.ToList();

			foreach (var item in itemsToRemove)
			{
				FamilyPreferences.Remove(item);
			}

			var existingIds = FamilyPreferences.Select(fp => fp.FamilyId).ToHashSet();
			var idsToAdd = distinctNewIds.Where(id => !existingIds.Contains(id));

			foreach (var familyId in idsToAdd)
			{
				FamilyPreferences.Add(CustomerFamilyPreference.Create(Id, familyId));
			}
		}

		public void UpdateAttributePreferences(IEnumerable<int>? attributePreferenceIds)
		{
			var distinctNewIds = attributePreferenceIds?.Distinct().ToList() ?? [];

			if (distinctNewIds.Any(id => id <= 0))
               throw DomainException.BadRequest("Tất cả ID sở thích thuộc tính phải lớn hơn 0.");

			var itemsToRemove = AttributePreferences
				.Where(ap => !distinctNewIds.Contains(ap.AttributeValueId))
				.ToList();

			foreach (var item in itemsToRemove)
			{
				AttributePreferences.Remove(item);
			}

			var existingIds = AttributePreferences.Select(ap => ap.AttributeValueId).ToHashSet();
			var idsToAdd = distinctNewIds.Where(id => !existingIds.Contains(id));

			foreach (var attributeId in idsToAdd)
			{
				AttributePreferences.Add(CustomerAttributePreference.Create(Id, attributeId));
			}
		}
	}
}
