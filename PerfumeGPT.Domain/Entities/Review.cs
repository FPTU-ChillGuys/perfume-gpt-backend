using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Review : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		protected Review() { }

		public Guid UserId { get; private set; }
		public Guid OrderDetailId { get; private set; }
		public int Rating { get; private set; }
		public string? Comment { get; private set; }

		// Staff response
		public string? StaffFeedbackComment { get; private set; }
		public Guid? StaffFeedbackByStaffId { get; private set; }
		public DateTime? StaffFeedbackAt { get; private set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual OrderDetail OrderDetail { get; set; } = null!;
		public virtual User? StaffFeedbackByStaff { get; set; }
		public virtual ICollection<Media> ReviewImages { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Review Create(Guid userId, Guid orderDetailId, int rating, string? comment)
		{
			if (userId == Guid.Empty)
			{
               throw DomainException.BadRequest("User ID là bắt buộc.");
			}

			if (orderDetailId == Guid.Empty)
			{
               throw DomainException.BadRequest("Order detail ID là bắt buộc.");
			}

			if (rating < 1 || rating > 5)
			{
              throw DomainException.BadRequest("Đánh giá phải nằm trong khoảng từ 1 đến 5 sao.");
			}

			return new Review
			{
				UserId = userId,
				OrderDetailId = orderDetailId,
				Rating = rating,
				Comment = comment?.Trim(),
			};
		}

		// Business logic methods
		public bool HasStaffResponse() => !string.IsNullOrWhiteSpace(StaffFeedbackComment);

		public void AnswerByStaff(Guid staffId, string staffFeedbackComment, DateTime answeredAtUtc)
		{
			if (staffId == Guid.Empty)
			{
              throw DomainException.BadRequest("Staff ID là bắt buộc.");
			}

			if (string.IsNullOrWhiteSpace(staffFeedbackComment))
			{
                throw DomainException.BadRequest("Nội dung phản hồi của nhân viên là bắt buộc.");
			}

			if (HasStaffResponse())
			{
              throw DomainException.BadRequest("Đánh giá này đã có phản hồi từ nhân viên.");
			}

			StaffFeedbackComment = staffFeedbackComment.Trim();
			StaffFeedbackByStaffId = staffId;
			StaffFeedbackAt = answeredAtUtc;
		}

		public bool IsAuthor(Guid userId) => UserId == userId;
	}
}
