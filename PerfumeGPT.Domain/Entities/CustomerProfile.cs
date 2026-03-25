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
				throw DomainException.BadRequest("User ID is required.");
			}

			return new CustomerProfile
			{
				UserId = userId
			};
		}

		// Business logic methods
		public void UpdateBasicInfo(DateTime? dateOfBirth, decimal? minBudget, decimal? maxBudget)
		{
			if (minBudget.HasValue && minBudget.Value < 0)
			{
				throw DomainException.BadRequest("MinBudget must be greater than or equal to 0.");
			}

			if (maxBudget.HasValue && maxBudget.Value < 0)
			{
				throw DomainException.BadRequest("MaxBudget must be greater than or equal to 0.");
			}

			if (minBudget.HasValue && maxBudget.HasValue && minBudget.Value > maxBudget.Value)
			{
				throw DomainException.BadRequest("MinBudget cannot be greater than MaxBudget.");
			}

			DateOfBirth = dateOfBirth;
			MinBudget = minBudget;
			MaxBudget = maxBudget;
		}

		public void UpdatePreferences(
			IEnumerable<int> notePreferenceIds,
			IEnumerable<int> familyPreferenceIds,
			IEnumerable<int> attributePreferenceIds)
		{
			var noteIds = notePreferenceIds?.Distinct().ToList() ?? [];
			var familyIds = familyPreferenceIds?.Distinct().ToList() ?? [];
			var attributeIds = attributePreferenceIds?.Distinct().ToList() ?? [];

			if (noteIds.Any(id => id <= 0))
			{
				throw DomainException.BadRequest("All note preference IDs must be greater than 0.");
			}

			if (familyIds.Any(id => id <= 0))
			{
				throw DomainException.BadRequest("All family preference IDs must be greater than 0.");
			}

			if (attributeIds.Any(id => id <= 0))
			{
				throw DomainException.BadRequest("All attribute preference IDs must be greater than 0.");
			}

            var existingNoteTypes = NotePreferences
				.GroupBy(x => x.NoteId)
				.ToDictionary(g => g.Key, g => g.First().NoteType);

			NotePreferences.Clear();
			foreach (var noteId in noteIds)
			{
             var noteType = existingNoteTypes.TryGetValue(noteId, out var existingType)
					? existingType
					: NoteType.Top;
				NotePreferences.Add(CustomerNotePreference.Create(Id, noteId, noteType));
			}

			FamilyPreferences.Clear();
			foreach (var familyId in familyIds)
			{
				FamilyPreferences.Add(CustomerFamilyPreference.Create(Id, familyId));
			}

			AttributePreferences.Clear();
			foreach (var attributeId in attributeIds)
			{
				AttributePreferences.Add(CustomerAttributePreference.Create(Id, attributeId));
			}
		}
	}
}
