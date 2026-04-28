using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class UserDeviceToken : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid UserId { get; set; }
		public string Token { get; set; } = null!;
		public string DeviceType { get; set; } = null!; // "iOS", "Android", "Web"
		public DateTime LastUsedAt { get; set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory method để tạo mới hoặc cập nhật
		public static UserDeviceToken Create(Guid userId, string token, string deviceType)
		{
			if (userId == Guid.Empty) throw DomainException.BadRequest("UserId là bắt buộc.");
			if (string.IsNullOrWhiteSpace(token)) throw DomainException.BadRequest("Token là bắt buộc.");

			return new UserDeviceToken
			{
				UserId = userId,
				Token = token.Trim(),
				DeviceType = deviceType?.Trim() ?? "Unknown",
				LastUsedAt = DateTime.UtcNow
			};
		}

		public void UpdateLastUsed()
		{
			LastUsedAt = DateTime.UtcNow;
		}
	}
}
