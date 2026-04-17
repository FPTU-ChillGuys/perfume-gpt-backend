using Microsoft.Data.SqlTypes;
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

		// Embedding
		public SqlVector<float>? Embedding { get; set; }

		// Factory method
		public static Product Create(ProductPayload payload)
		{
			ValidateCore(payload);

			return new Product
			{
				Name = payload.Name.Trim(),
				BrandId = payload.BrandId,
				CategoryId = payload.CategoryId,
				Origin = payload.Origin.Trim(),
				Gender = payload.Gender,
				ReleaseYear = payload.ReleaseYear,
				Description = payload.Description?.Trim()
			};
		}

		// Business logic methods
		public void Update(UpdateProductPayload payload)
		{
			if (payload.Name != null)
			{
				if (string.IsNullOrWhiteSpace(payload.Name))
                  throw DomainException.BadRequest("Tên sản phẩm không được để trống.");
				Name = payload.Name.Trim();
			}

			if (payload.BrandId.HasValue)
			{
				if (payload.BrandId.Value <= 0)
                 throw DomainException.BadRequest("Thương hiệu không hợp lệ.");
				BrandId = payload.BrandId.Value;
			}

			if (payload.CategoryId.HasValue)
			{
				if (payload.CategoryId.Value <= 0)
                  throw DomainException.BadRequest("Danh mục không hợp lệ.");
				CategoryId = payload.CategoryId.Value;
			}

			if (payload.Origin != null)
			{
				if (string.IsNullOrWhiteSpace(payload.Origin))
                    throw DomainException.BadRequest("Xuất xứ không được để trống.");
				Origin = payload.Origin.Trim();
			}

			if (payload.Gender.HasValue)
				Gender = payload.Gender.Value;

			if (payload.ReleaseYear.HasValue)
			{
				if (payload.ReleaseYear.Value < 1900 || payload.ReleaseYear.Value > DateTime.UtcNow.Year + 1)
                  throw DomainException.BadRequest("Năm phát hành không hợp lệ.");
				ReleaseYear = payload.ReleaseYear.Value;
			}

			if (payload.Description != null)
				Description = payload.Description.Trim();
		}

		public void ReplaceScentMaps(IEnumerable<(int NoteId, NoteType Type)> scentNotes)
		{
			if (scentNotes == null)
              throw DomainException.BadRequest("Danh sách nốt hương là bắt buộc.");

			ProductScentMaps.Clear();
			foreach (var (noteId, type) in scentNotes)
				ProductScentMaps.Add(ProductNoteMap.Create(noteId, type));
		}

		public void ReplaceFamilyMaps(IEnumerable<int> olfactoryFamilyIds)
		{
			if (olfactoryFamilyIds == null)
               throw DomainException.BadRequest("Danh sách nhóm hương là bắt buộc.");

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

		public void UpdateEmbedding(SqlVector<float> embedding)
		{
			Embedding = embedding;
		}

		public void EnsureNotDeleted()
		{
			if (IsDeleted)
               throw DomainException.BadRequest("Không thể cập nhật sản phẩm đã bị xóa.");
		}

		public static void EnsureCanBeDeleted(bool hasActiveVariants)
		{
			if (hasActiveVariants)
				throw DomainException.BadRequest(
                   "Không thể xóa sản phẩm có biến thể đang hoạt động. Vui lòng xóa tất cả biến thể trước.");
		}

		private static void ValidateCore(ProductPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.Name))
              throw DomainException.BadRequest("Tên sản phẩm là bắt buộc.");

			if (payload.BrandId <= 0)
             throw DomainException.BadRequest("Thương hiệu không hợp lệ.");

			if (payload.CategoryId <= 0)
              throw DomainException.BadRequest("Danh mục không hợp lệ.");

			if (string.IsNullOrWhiteSpace(payload.Origin))
                throw DomainException.BadRequest("Xuất xứ sản phẩm là bắt buộc.");

			if (payload.ReleaseYear < 1900 || payload.ReleaseYear > DateTime.UtcNow.Year + 1)
              throw DomainException.BadRequest("Năm phát hành không hợp lệ.");
		}

		// Records
		public record ProductPayload
		{
			public required string Name { get; init; }
			public required int BrandId { get; init; }
			public required int CategoryId { get; init; }
			public required string Origin { get; init; }
			public required Gender Gender { get; init; }
			public required int ReleaseYear { get; init; }
			public string? Description { get; init; }
		}

		public record UpdateProductPayload
		{
			public string? Name { get; init; }
			public int? BrandId { get; init; }
			public int? CategoryId { get; init; }
			public string? Origin { get; init; }
			public Gender? Gender { get; init; }
			public int? ReleaseYear { get; init; }
			public string? Description { get; init; }
		}
	}
}
