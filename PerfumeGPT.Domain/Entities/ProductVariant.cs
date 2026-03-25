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

		// Navigation
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

		// ISoftDelete
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory method
		public static ProductVariant Create(
			Guid productId,
			string barcode,
			string sku,
			int volumeMl,
			int concentrationId,
			VariantType type,
			int sillage,
			int longevity,
			decimal basePrice,
			decimal? retailPrice = null,
			VariantStatus status = VariantStatus.Active)
		{
			if (productId == Guid.Empty)
				throw DomainException.BadRequest("Product ID is required.");

			if (string.IsNullOrWhiteSpace(barcode))
				throw DomainException.BadRequest("Barcode is required.");

			if (string.IsNullOrWhiteSpace(sku))
				throw DomainException.BadRequest("SKU is required.");

			if (volumeMl <= 0)
				throw DomainException.BadRequest("Volume must be greater than 0.");

			if (concentrationId <= 0)
				throw DomainException.BadRequest("Invalid concentration.");

			if (basePrice <= 0)
				throw DomainException.BadRequest("Base price must be greater than 0.");

			if (retailPrice.HasValue && retailPrice.Value <= 0)
				throw DomainException.BadRequest("Retail price must be greater than 0.");

			return new ProductVariant
			{
				ProductId = productId,
				Barcode = barcode.Trim(),
				Sku = sku.Trim().ToUpperInvariant(),
				VolumeMl = volumeMl,
				ConcentrationId = concentrationId,
				Type = type,
				Sillage = sillage,
				Longevity = longevity,
				BasePrice = basePrice,
				RetailPrice = retailPrice,
				Status = status
			};
		}

		// Domain methods
		public void Update(
			string? barcode,
			string? sku,
			int? volumeMl,
			int? concentrationId,
			VariantType? type,
			int? sillage,
			int? longevity,
			decimal? basePrice,
			decimal? retailPrice,
			VariantStatus? status)
		{
			if (barcode != null)
			{
				if (string.IsNullOrWhiteSpace(barcode))
					throw DomainException.BadRequest("Barcode cannot be empty.");
				Barcode = barcode.Trim();
			}

			if (sku != null)
			{
				if (string.IsNullOrWhiteSpace(sku))
					throw DomainException.BadRequest("SKU cannot be empty.");
				Sku = sku.Trim().ToUpperInvariant();
			}

			if (volumeMl.HasValue)
			{
				if (volumeMl.Value <= 0)
					throw DomainException.BadRequest("Volume must be greater than 0.");
				VolumeMl = volumeMl.Value;
			}

			if (concentrationId.HasValue)
			{
				if (concentrationId.Value <= 0)
					throw DomainException.BadRequest("Invalid concentration.");
				ConcentrationId = concentrationId.Value;
			}

			if (type.HasValue) Type = type.Value;
			if (sillage.HasValue) Sillage = sillage.Value;
			if (longevity.HasValue) Longevity = longevity.Value;

			if (basePrice.HasValue)
			{
				if (basePrice.Value <= 0)
					throw DomainException.BadRequest("Base price must be greater than 0.");
				BasePrice = basePrice.Value;
			}

			if (retailPrice.HasValue)
			{
				if (retailPrice.Value <= 0)
					throw DomainException.BadRequest("Retail price must be greater than 0.");
				RetailPrice = retailPrice.Value;
			}

			if (status.HasValue) Status = status.Value;
		}

		public void EnsureNotDeleted()
		{
			if (IsDeleted)
				throw DomainException.BadRequest("Cannot update a deleted variant.");
		}

		public void EnsureAvailableForCart()
		{
			if (IsDeleted)
				throw DomainException.BadRequest("This product variant is no longer available.");

			if (Status == VariantStatus.Discontinued)
				throw DomainException.BadRequest("This product variant has been discontinued.");

			if (Status == VariantStatus.Inactive)
				throw DomainException.BadRequest("This product variant is currently inactive.");

			if (Status == VariantStatus.out_of_stock)
				throw DomainException.BadRequest("This product variant is out of stock.");
		}

		// Dùng cho ValidateVariantForCart nếu cần result thay vì exception
		public (bool IsValid, string? ErrorMessage) ValidateForCart()
		{
			if (IsDeleted) return (false, "This product variant is no longer available.");
			if (Status == VariantStatus.Discontinued) return (false, "This product variant has been discontinued.");
			if (Status == VariantStatus.Inactive) return (false, "This product variant is currently inactive.");
			if (Status == VariantStatus.out_of_stock) return (false, "This product variant is out of stock.");
			return (true, null);
		}
	}
}
