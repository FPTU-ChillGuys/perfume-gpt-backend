namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
    public class UpdateCodRequest
    {
        public string OrderCode { get; set; } = null!;
        public int CodAmount { get; set; }
    }
}
