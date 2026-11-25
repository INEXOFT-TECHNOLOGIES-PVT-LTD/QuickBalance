namespace QuickBalance.API.DTOs.Respons_DTO
{
    public class CustomerPendingBillResponse
    {
        public int Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? Narration { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string? InvoiceType { get; set; }
        public int? LogCode { get; set; }
        public bool BillRefFlag { get; set; }
        public decimal ConversionRate { get; set; }
        public int CurrencyCode { get; set; }
        public int BranchId { get; set; }
        public string? DrCr { get; set; }
    }
}
