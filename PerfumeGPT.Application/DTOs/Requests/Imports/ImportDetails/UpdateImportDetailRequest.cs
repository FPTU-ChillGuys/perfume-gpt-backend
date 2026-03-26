namespace PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails
{
	public class UpdateImportDetailRequest
	{
		public Guid? Id { get; set; }
		public Guid VariantId { get; set; }
		public int ExpectedQuantity { get; set; }
		public decimal UnitPrice { get; set; }
	}
}
