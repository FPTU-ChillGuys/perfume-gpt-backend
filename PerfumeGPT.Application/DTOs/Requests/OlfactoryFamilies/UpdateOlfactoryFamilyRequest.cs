using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Application.DTOs.Requests.OlfactoryFamilies
{
	public class UpdateOlfactoryFamilyRequest
	{
		[Required]
		public string Name { get; set; } = null!;
	}
}