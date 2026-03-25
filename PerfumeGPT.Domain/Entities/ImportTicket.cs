using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportTicket : BaseEntity<Guid>, IUpdateAuditable, IHasCreatedAt, ISoftDelete
	{
		protected ImportTicket() { }

		public Guid CreatedById { get; private set; }
		public Guid? VerifiedById { get; private set; }
		public int SupplierId { get; private set; }
		public DateTime ExpectedArrivalDate { get; private set; }
		public DateTime ActualImportDate { get; private set; }
		public decimal TotalCost { get; private set; }
		public ImportStatus Status { get; private set; }

		// Navigation properties
		public virtual User CreatedByUser { get; set; } = null!;
		public virtual User? VerifiedByUser { get; set; }
		public virtual Supplier Supplier { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];

		// IUpdateAuditable and IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// Factory methods
		public static ImportTicket Create(Guid createdById, int supplierId, DateTime expectedArrivalDate, decimal totalCost)
		{
			if (createdById == Guid.Empty)
				throw DomainException.BadRequest("Created by user is required.");

			if (supplierId <= 0)
				throw DomainException.BadRequest("Supplier is required.");

			if (totalCost < 0)
				throw DomainException.BadRequest("Total cost cannot be negative.");

			return new ImportTicket
			{
				CreatedById = createdById,
				SupplierId = supplierId,
				ExpectedArrivalDate = expectedArrivalDate,
				TotalCost = totalCost,
				Status = ImportStatus.Pending,
				ActualImportDate = DateTime.MinValue
			};
		}

		// Business ogc methods
		public void AddDetail(ImportDetail detail)
		{
			if (detail == null)
				throw DomainException.BadRequest("Import detail is required.");

			ImportDetails.Add(detail);
		}

		public void UpdateForPending(int supplierId, DateTime expectedArrivalDate, decimal totalCost)
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Only pending import tickets can be updated.");

			if (supplierId <= 0)
				throw DomainException.BadRequest("Supplier is required.");

			if (totalCost < 0)
				throw DomainException.BadRequest("Total cost cannot be negative.");

			SupplierId = supplierId;
			ExpectedArrivalDate = expectedArrivalDate;
			TotalCost = totalCost;
		}

		public void Complete(Guid verifiedById, DateTime actualImportDate)
		{
			if (Status != ImportStatus.InProgress)
				throw DomainException.BadRequest("Only in progress import tickets can be verified.");

			if (verifiedById == Guid.Empty)
				throw DomainException.BadRequest("Verified by user is required.");

			VerifiedById = verifiedById;
			ActualImportDate = actualImportDate;
			Status = ImportStatus.Completed;
		}

		public void UpdateStatus(ImportStatus newStatus)
		{
			if (Status == ImportStatus.Completed)
				throw DomainException.BadRequest("Completed import tickets are immutable. Create an adjustment ticket if needed.");

			if (Status == ImportStatus.Canceled)
				throw DomainException.BadRequest("Cancelled import tickets are read-only.");

			if (Status == ImportStatus.InProgress && newStatus != ImportStatus.Canceled)
				throw DomainException.BadRequest("Import ticket is locked while in progress. Complete verification or cancel it first.");

			if (Status == ImportStatus.Pending && newStatus != ImportStatus.InProgress && newStatus != ImportStatus.Canceled)
				throw DomainException.BadRequest("Pending tickets can only transition to InProgress or Canceled status.");

			Status = newStatus;
		}

		public void EnsureDeletable()
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Only pending import tickets can be deleted.");
		}
	}
}
