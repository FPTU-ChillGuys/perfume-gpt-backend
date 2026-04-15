using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductVariant : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		private ProductVariant() { }

		public Guid ProductId { get; private set; }
		public string Barcode { get; private set; } = null!;
		public string Sku { get; private set; } = null!;
		public int VolumeMl { get; private set; }
		public int ConcentrationId { get; private set; }
		public VariantType Type { get; private set; }
		public int Sillage { get; private set; }
		public int Longevity { get; private set; }
		public decimal BasePrice { get; private set; }
		public decimal? RetailPrice { get; private set; }
		public VariantStatus Status { get; private set; }
		public ReplenishmentPolicy RestockPolicy { get; private set; }

		// Navigation properties
		public virtual Product Product { get; set; } = null!;
		public virtual Concentration Concentration { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<Batch> Batches { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual Stock Stock { get; set; } = null!;
		public virtual ICollection<CartItem> CartItems { get; set; } = [];
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<Media> Media { get; set; } = [];
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<PromotionItem> PromotionItems { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory method
		public static ProductVariant Create(Guid productId, VariantPayload payload)
		{
			if (productId == Guid.Empty)
				throw DomainException.BadRequest("Product ID is required.");

			ValidateCore(payload);

			return new ProductVariant
			{
				ProductId = productId,
				Barcode = payload.Barcode.Trim(),
				Sku = payload.Sku.Trim().ToUpperInvariant(),
				VolumeMl = payload.VolumeMl,
				ConcentrationId = payload.ConcentrationId,
				Type = payload.Type,
				Sillage = payload.Sillage,
				Longevity = payload.Longevity,
				BasePrice = payload.BasePrice,
				RetailPrice = payload.RetailPrice,
				Status = payload.Status,
				RestockPolicy = payload.RestockPolicy
			};
		}

		// Business logic methods
		public void Update(UpdateVariantPayload payload)
		{
			if (payload.Barcode != null)
			{
				if (string.IsNullOrWhiteSpace(payload.Barcode))
					throw DomainException.BadRequest("Barcode cannot be empty.");
				Barcode = payload.Barcode.Trim();
			}

			if (payload.Sku != null)
			{
				if (string.IsNullOrWhiteSpace(payload.Sku))
					throw DomainException.BadRequest("SKU cannot be empty.");
				Sku = payload.Sku.Trim().ToUpperInvariant();
			}

			if (payload.VolumeMl.HasValue)
			{
				if (payload.VolumeMl.Value <= 0)
					throw DomainException.BadRequest("Volume must be greater than 0.");
				VolumeMl = payload.VolumeMl.Value;
			}

			if (payload.ConcentrationId.HasValue)
			{
				if (payload.ConcentrationId.Value <= 0)
					throw DomainException.BadRequest("Invalid concentration.");
				ConcentrationId = payload.ConcentrationId.Value;
			}

			if (payload.Type.HasValue) Type = payload.Type.Value;
			if (payload.Sillage.HasValue) Sillage = payload.Sillage.Value;
			if (payload.Longevity.HasValue) Longevity = payload.Longevity.Value;

			if (payload.BasePrice.HasValue)
			{
				if (payload.BasePrice.Value <= 0)
					throw DomainException.BadRequest("Base price must be greater than 0.");
				BasePrice = payload.BasePrice.Value;
			}

			if (payload.RetailPrice.HasValue)
			{
				if (payload.RetailPrice.Value <= 0)
					throw DomainException.BadRequest("Retail price must be greater than 0.");
				RetailPrice = payload.RetailPrice.Value;
			}

			if (payload.Status.HasValue) Status = payload.Status.Value;
			if (payload.RestockPolicy.HasValue) RestockPolicy = payload.RestockPolicy.Value;
		}

		public void ApplyStockPolicy(int totalQuantity)
		{
			if (RestockPolicy == ReplenishmentPolicy.DoNotRestock && totalQuantity <= 0)
			{
				Status = VariantStatus.Discontinued;
			}
		}

		public void EnsureNotDeleted()
		{
			if (IsDeleted)
				throw DomainException.BadRequest("Cannot update a deleted variant.");
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
					: ProductAttribute.CreateForVariant(Id, AttributeId, ValueId));
			}
		}

		public void EnsureAvailableForCart()
		{
			if (IsDeleted)
				throw DomainException.BadRequest("This product variant is no longer available.");

			if (Status == VariantStatus.Discontinued)
				throw DomainException.BadRequest("This product variant has been discontinued.");

			if (Status == VariantStatus.Inactive)
				throw DomainException.BadRequest("This product variant is currently inactive.");
		}

		private static void ValidateCore(VariantPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.Barcode))
				throw DomainException.BadRequest("Barcode is required.");

			if (string.IsNullOrWhiteSpace(payload.Sku))
				throw DomainException.BadRequest("SKU is required.");

			if (payload.VolumeMl <= 0)
				throw DomainException.BadRequest("Volume must be greater than 0.");

			if (payload.ConcentrationId <= 0)
				throw DomainException.BadRequest("Invalid concentration.");

			if (payload.BasePrice <= 0)
				throw DomainException.BadRequest("Base price must be greater than 0.");

			if (payload.RetailPrice.HasValue && payload.RetailPrice.Value <= 0)
				throw DomainException.BadRequest("Retail price must be greater than 0.");
		}

		// Records
		public record VariantPayload
		{
			public required string Barcode { get; init; }
			public required string Sku { get; init; }
			public required int VolumeMl { get; init; }
			public required int ConcentrationId { get; init; }
			public required VariantType Type { get; init; }
			public required int Sillage { get; init; }
			public required int Longevity { get; init; }
			public required decimal BasePrice { get; init; }
			public decimal? RetailPrice { get; init; }
			public VariantStatus Status { get; init; } = VariantStatus.Active;
			public ReplenishmentPolicy RestockPolicy { get; init; } = ReplenishmentPolicy.AutoRestock;
		}

		public record UpdateVariantPayload
		{
			public string? Barcode { get; init; }
			public string? Sku { get; init; }
			public int? VolumeMl { get; init; }
			public int? ConcentrationId { get; init; }
			public VariantType? Type { get; init; }
			public int? Sillage { get; init; }
			public int? Longevity { get; init; }
			public decimal? BasePrice { get; init; }
			public decimal? RetailPrice { get; init; }
			public VariantStatus? Status { get; init; }
			public ReplenishmentPolicy? RestockPolicy { get; init; }
		}
	}
}
