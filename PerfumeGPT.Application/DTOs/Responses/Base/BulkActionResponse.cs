namespace PerfumeGPT.Application.DTOs.Responses.Base
{
	public class BulkActionResponse
	{
		public List<Guid> SucceededIds { get; set; } = [];
		public List<BulkActionError> FailedItems { get; set; } = [];

		public int TotalProcessed => SucceededIds.Count + FailedItems.Count;
		public bool HasError => FailedItems.Any();
	}

	public class BulkActionError
	{
		public Guid Id { get; set; }
		public string ErrorMessage { get; set; } = null!;
	}
}
