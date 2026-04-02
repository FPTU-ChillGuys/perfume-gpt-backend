using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;
using static PerfumeGPT.Domain.Entities.ImportDetail;

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
		public static ImportTicket Create(Guid createdById, ImportHeader header)
		{
			if (createdById == Guid.Empty)
				throw DomainException.BadRequest("Created by user is required.");

			if (header.SupplierId <= 0)
				throw DomainException.BadRequest("Supplier is required.");

			if (header.TotalCost < 0)
				throw DomainException.BadRequest("Total cost cannot be negative.");

			return new ImportTicket
			{
				CreatedById = createdById,
				SupplierId = header.SupplierId,
				ExpectedArrivalDate = header.ExpectedArrivalDate,
				TotalCost = header.TotalCost,
				Status = ImportStatus.Pending
			};
		}

		// Business logc methods
		public void AddDetail(ImportDetail detail)
		{
			if (detail == null)
				throw DomainException.BadRequest("Import detail is required.");

			ImportDetails.Add(detail);
		}

		public void UpdateDetail(Guid detailId, ImportItemInfo info)
		{
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId)
				?? throw DomainException.NotFound($"Import detail with ID {detailId} does not exist in this ticket.");

			detail.UpdateExpected(info);
		}

		public void RemoveDetail(Guid detailId)
		{
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId)
				?? throw DomainException.NotFound($"Import detail with ID {detailId} does not exist in this ticket.");

			ImportDetails.Remove(detail);
		}

		public void VerifyDetail(Guid detailId, DetailVerification verification)
		{
			if (Status != ImportStatus.InProgress)
				throw DomainException.BadRequest("Cannot verify details unless the import ticket is in progress.");
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId) ?? throw DomainException.NotFound($"Import detail with ID {detailId} does not exist in this ticket.");

			detail.Verify(verification);
		}

		public void UpdateForPending(ImportHeader header)
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Only pending import tickets can be updated.");

			if (header.SupplierId <= 0)
				throw DomainException.BadRequest("Supplier is required.");

			if (header.TotalCost < 0)
				throw DomainException.BadRequest("Total cost cannot be negative.");

			SupplierId = header.SupplierId;
			ExpectedArrivalDate = header.ExpectedArrivalDate;
			TotalCost = header.TotalCost;
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

			if (Status == ImportStatus.Cancelled)
				throw DomainException.BadRequest("Cancelled import tickets are read-only.");

			if (Status == ImportStatus.InProgress && newStatus != ImportStatus.Cancelled)
				throw DomainException.BadRequest("Import ticket is locked while in progress. Complete verification or cancel it first.");

			if (Status == ImportStatus.Pending && newStatus != ImportStatus.InProgress && newStatus != ImportStatus.Cancelled)
				throw DomainException.BadRequest("Pending tickets can only transition to InProgress or Canceled status.");

			Status = newStatus;
		}

		public void EnsureIsPendingStatus()
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Only pending import tickets can be deleted.");
		}

		// Records
		public record ImportHeader(
			int SupplierId,
			DateTime ExpectedArrivalDate,
			decimal TotalCost
		);
	}
}
