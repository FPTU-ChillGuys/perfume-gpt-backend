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
				throw DomainException.BadRequest("Id người tạo là bắt buộc.");

			if (header.SupplierId <= 0)
				throw DomainException.BadRequest("Nhà cung cấp là bắt buộc và không được để trống.");

			if (header.TotalCost < 0)
				throw DomainException.BadRequest("Tổng chi phí không được âm.");

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
				throw DomainException.BadRequest("Chi tiết nhập hàng là bắt buộc và không được để trống.");

			ImportDetails.Add(detail);
		}

		public void UpdateDetail(Guid detailId, ImportItemInfo info)
		{
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId)
				?? throw DomainException.NotFound($"Chi tiết nhập hàng với ID {detailId} không tồn tại trong phiếu này.");

			detail.UpdateExpected(info);
		}

		public void RemoveDetail(Guid detailId)
		{
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId)
				?? throw DomainException.NotFound($"Chi tiết nhập hàng với ID {detailId} không tồn tại trong phiếu này.");

			ImportDetails.Remove(detail);
		}

		public void VerifyDetail(Guid detailId, DetailVerification verification)
		{
			if (Status != ImportStatus.InProgress)
				throw DomainException.BadRequest("Không thể xác minh chi tiết nhập hàng trừ khi phiếu nhập đang trong tiến trình.");
			ImportDetails ??= [];
			var detail = ImportDetails.FirstOrDefault(d => d.Id == detailId) ?? throw DomainException.NotFound($"Chi tiết nhập hàng với ID {detailId} không tồn tại trong phiếu này.");

			detail.Verify(verification);
		}

		public void UpdateForPending(ImportHeader header)
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Chỉ các phiếu nhập đang chờ xử lý mới có thể được cập nhật.");

			if (header.SupplierId <= 0)
				throw DomainException.BadRequest("Nhà cung cấp là bắt buộc và không được để trống.");

			if (header.TotalCost < 0)
				throw DomainException.BadRequest("Tổng chi phí không được âm.");

			SupplierId = header.SupplierId;
			ExpectedArrivalDate = header.ExpectedArrivalDate;
			TotalCost = header.TotalCost;
		}

		public void Complete(Guid verifiedById, DateTime actualImportDate)
		{
			if (Status != ImportStatus.InProgress)
				throw DomainException.BadRequest("Chỉ các phiếu nhập đang trong tiến trình mới có thể được xác minh.");

			if (verifiedById == Guid.Empty)
				throw DomainException.BadRequest("Người xác minh là bắt buộc và không được để trống.");

			VerifiedById = verifiedById;
			ActualImportDate = actualImportDate;
			Status = ImportStatus.Completed;
		}

		public void UpdateStatus(ImportStatus newStatus)
		{
			if (Status == ImportStatus.Completed)
				throw DomainException.BadRequest("Các phiếu nhập đã hoàn thành không thể thay đổi. Tạo phiếu điều chỉnh nếu cần.");

			if (Status == ImportStatus.Cancelled)
				throw DomainException.BadRequest("Các phiếu nhập đã hủy không thể chỉnh sửa.");

			if (Status == ImportStatus.InProgress && newStatus != ImportStatus.Cancelled)
				throw DomainException.BadRequest("Các phiếu nhập đang trong tiến trình không thể chỉnh sửa. Hoàn tất xác minh hoặc hủy trước.");

			if (Status == ImportStatus.Pending && newStatus != ImportStatus.InProgress && newStatus != ImportStatus.Cancelled)
				throw DomainException.BadRequest("Các phiếu nhập đang chờ xử lý chỉ có thể chuyển sang trạng thái Đang tiến hành hoặc Đã hủy.");

			Status = newStatus;
		}

		public void EnsureIsPendingStatus()
		{
			if (Status != ImportStatus.Pending)
				throw DomainException.BadRequest("Chỉ các phiếu nhập đang chờ xử lý mới có thể bị xóa.");
		}

		// Records
		public record ImportHeader(
			int SupplierId,
			DateTime ExpectedArrivalDate,
			decimal TotalCost
		);
	}
}
