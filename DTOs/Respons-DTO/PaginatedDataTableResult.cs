namespace QuickBalance.API.DTOs.Respons_DTO
{
    using System.Data;

    public class PaginatedDataTableResult
    {
        public dynamic? Data { get; set; }
        public int TotalRecords { get; set; }
    }
}
