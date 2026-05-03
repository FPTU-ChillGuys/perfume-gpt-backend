namespace PerfumeGPT.Application.DTOs.Commons
{
	/// <summary>
	/// Tham số tính tồn bán được theo lô (buffer ngừng bán thường + buffer xả kho + danh sách lô clearance).
	/// </summary>
	public sealed record SellableStockQueryContext(
		DateTime NormalSellableAfterUtc,
		DateTime ClearanceSellableAfterUtc,
		HashSet<Guid> ClearanceBatchIds);

}
