using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies
{
	public class CreateOlfactoryFamilyRequest
	{
		[Required]
		public string Name { get; set; } = null!;
	}
}