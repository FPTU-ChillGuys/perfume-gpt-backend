using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportDetail : BaseEntity<Guid>
	{
		protected ImportDetail() { }

		public Guid ImportId { get; private set; }
		public Guid ProductVariantId { get; private set; }
		public int ExpectedQuantity { get; private set; }
		public decimal UnitPrice { get; private set; }
		public int RejectedQuantity { get; private set; } = 0;
		public string? Note { get; private set; }

		// Navigation properties
		public virtual ImportTicket ImportTicket { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ICollection<Batch> Batches { get; set; } = [];

		// Factory methods
		public static ImportDetail Create(Guid productVariantId, int expectedQuantity, decimal unitPrice)
		{
			ValidateVariantId(productVariantId);
			ValidateExpectedQuantity(expectedQuantity);
			ValidateUnitPrice(unitPrice);

			return new ImportDetail
			{
				ProductVariantId = productVariantId,
				ExpectedQuantity = expectedQuantity,
				UnitPrice = unitPrice,
				RejectedQuantity = 0
			};
		}

		// Business logic methods
		public void UpdateExpected(Guid productVariantId, int expectedQuantity, decimal unitPrice)
		{
			ValidateVariantId(productVariantId);
			ValidateExpectedQuantity(expectedQuantity);
			ValidateUnitPrice(unitPrice);

			ProductVariantId = productVariantId;
			ExpectedQuantity = expectedQuantity;
			UnitPrice = unitPrice;
		}

		public void Verify(int rejectedQuantity, string? note)
		{
			if (rejectedQuantity < 0)
				throw DomainException.BadRequest("Rejected quantity cannot be negative.");

			if (rejectedQuantity > ExpectedQuantity)
				throw DomainException.BadRequest("Rejected quantity cannot exceed expected quantity.");

			RejectedQuantity = rejectedQuantity;
			Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
		}

		private static void ValidateVariantId(Guid productVariantId)
		{
			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");
		}

		private static void ValidateExpectedQuantity(int expectedQuantity)
		{
			if (expectedQuantity <= 0)
				throw DomainException.BadRequest("Expected quantity must be greater than 0.");
		}

		private static void ValidateUnitPrice(decimal unitPrice)
		{
			if (unitPrice <= 0)
				throw DomainException.BadRequest("Unit price must be greater than 0.");
		}
	}
}
