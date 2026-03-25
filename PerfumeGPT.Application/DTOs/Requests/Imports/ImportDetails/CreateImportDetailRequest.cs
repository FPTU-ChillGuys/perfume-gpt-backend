namespace PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails
{
	public class CreateImportDetailRequest
	{
		public Guid VariantId { get; set; }
		public int ExpectedQuantity { get; set; }
		public decimal UnitPrice { get; set; }
	}
}
