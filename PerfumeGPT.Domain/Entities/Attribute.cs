using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Helpers;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Attribute : BaseEntity<int>
	{
		private Attribute() { }

		public string InternalCode { get; private set; } = null!;
		public string Name { get; private set; } = null!;
		public string? Description { get; private set; }
		public bool IsVariantLevel { get; private set; }

		// Navigation properties
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<AttributeValue> AttributeValues { get; set; } = [];

		// Factory method
		public static Attribute Create(AttributeCreationDetails details)
		{
			if (string.IsNullOrWhiteSpace(details.Name))
				throw DomainException.BadRequest("Tên thuộc tính không được để trống.");

			string finalCode = string.IsNullOrWhiteSpace(details.InternalCode)
				? GenerateInternalCode(details.Name)
				: details.InternalCode.Trim().ToUpperInvariant();

			return new Attribute
			{
				InternalCode = finalCode,
				Name = details.Name.Trim(),
				Description = details.Description?.Trim(),
				IsVariantLevel = details.IsVariantLevel
			};
		}

		public void Update(AttributeUpdateDetails details)
		{
			if (string.IsNullOrWhiteSpace(details.Name))
				throw DomainException.BadRequest("Tên thuộc tính không được để trống.");

			Name = details.Name.Trim();
			Description = details.Description?.Trim();
			IsVariantLevel = details.IsVariantLevel;
		}

		private static string GenerateInternalCode(string name) => name.ToUrlsFriendly().ToUpperInvariant();

		// Records
		public record AttributeCreationDetails(
			string? InternalCode,
			string Name,
			string? Description,
			bool IsVariantLevel
		);

		public record AttributeUpdateDetails(
			string Name,
			string? Description,
			bool IsVariantLevel
		);
	}
}
