using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;


namespace PerfumeGPT.Domain.Entities
{
	public class Product : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		private Product() { }

		public string Name { get; private set; } = null!;
		public int BrandId { get; private set; }
		public int CategoryId { get; private set; }
		public string? Description { get; private set; }
		public string Origin { get; private set; } = null!;
		public Gender Gender { get; private set; }
		public int ReleaseYear { get; private set; }

		// Navigation properties
		public virtual Brand Brand { get; set; } = null!;
		public virtual Category Category { get; set; } = null!;
		public virtual ICollection<ProductVariant> Variants { get; set; } = [];
		public virtual ICollection<Media> Media { get; set; } = [];
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<ProductNoteMap> ProductScentMaps { get; set; } = [];
		public virtual ICollection<ProductFamilyMap> ProductFamilyMaps { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }



		// Factory method
		public static Product Create(
			string name,
			int brandId,
			int categoryId,
			string origin,
			Gender gender,
			int releaseYear,
			string? description = null)
		{
			ValidateCore(name, brandId, categoryId, origin, releaseYear);

			return new Product
			{
				Name = name.Trim(),
				BrandId = brandId,
				CategoryId = categoryId,
				Origin = origin.Trim(),
				Gender = gender,
				ReleaseYear = releaseYear,
				Description = description?.Trim()
			};
		}

		// Business logic methods
		public void Update(
			string? name,
			int? brandId,
			int? categoryId,
			string? origin,
			Gender? gender,
			int? releaseYear,
			string? description)
		{
			if (name != null)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw DomainException.BadRequest("Product name cannot be empty.");
				Name = name.Trim();
			}

			if (brandId.HasValue)
			{
				if (brandId.Value <= 0)
					throw DomainException.BadRequest("Invalid brand.");
				BrandId = brandId.Value;
			}

			if (categoryId.HasValue)
			{
				if (categoryId.Value <= 0)
					throw DomainException.BadRequest("Invalid category.");
				CategoryId = categoryId.Value;
			}

			if (origin != null)
			{
				if (string.IsNullOrWhiteSpace(origin))
					throw DomainException.BadRequest("Origin cannot be empty.");
				Origin = origin.Trim();
			}

			if (gender.HasValue) Gender = gender.Value;

			if (releaseYear.HasValue)
			{
				if (releaseYear.Value < 1900 || releaseYear.Value > DateTime.UtcNow.Year + 1)
					throw DomainException.BadRequest("Invalid release year.");
				ReleaseYear = releaseYear.Value;
			}

			if (description != null) Description = description.Trim();
		}

		public void ReplaceScentMaps(IEnumerable<(int NoteId, NoteType Type)> scentNotes)
		{
			if (scentNotes == null)
				throw DomainException.BadRequest("Scent notes are required.");

			ProductScentMaps.Clear();
			foreach (var (noteId, type) in scentNotes)
				ProductScentMaps.Add(ProductNoteMap.Create(noteId, type));
		}

		public void ReplaceFamilyMaps(IEnumerable<int> olfactoryFamilyIds)
		{
			if (olfactoryFamilyIds == null)
				throw DomainException.BadRequest("Olfactory families are required.");

			ProductFamilyMaps.Clear();
			foreach (var familyId in olfactoryFamilyIds)
				ProductFamilyMaps.Add(ProductFamilyMap.Create(familyId));
		}

		public void SyncAttributes(IEnumerable<(int AttributeId, int ValueId)> newAttributes)
		{
			ProductAttributes ??= [];
			ProductAttributes.Clear();

			if (newAttributes == null)
				return;

			foreach (var (AttributeId, ValueId) in newAttributes)
			{
				ProductAttributes.Add(Id == Guid.Empty
					? ProductAttribute.Create(AttributeId, ValueId)
					: ProductAttribute.CreateForProduct(Id, AttributeId, ValueId));
			}
		}

		public void EnsureNotDeleted()
		{
			if (IsDeleted)
				throw DomainException.BadRequest("Cannot update a deleted product.");
		}

		public static void EnsureCanBeDeleted(bool hasActiveVariants)
		{
			if (hasActiveVariants)
				throw DomainException.BadRequest(
					"Cannot delete product with active variants. Please delete all variants first.");
		}

		private static void ValidateCore(
			string name, int brandId, int categoryId, string origin, int releaseYear)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw DomainException.BadRequest("Product name is required.");

			if (brandId <= 0)
				throw DomainException.BadRequest("Invalid brand.");

			if (categoryId <= 0)
				throw DomainException.BadRequest("Invalid category.");

			if (string.IsNullOrWhiteSpace(origin))
				throw DomainException.BadRequest("Product origin is required.");

			if (releaseYear < 1900 || releaseYear > DateTime.UtcNow.Year + 1)
				throw DomainException.BadRequest("Invalid release year.");
		}
	}
}
