namespace QuickBalance.API.DTOs
{
    public class SalesInvoiceRequest
    {
        public int SinvNo { get; set; }

        public DateTime Date { get; set; }

        public int CustomerCode { get; set; }

        public string? PayMode { get; set; }

        public int CashLedgerCode { get; set; }

        public int BankLedgerCode { get; set; }

        public DateTime? DueDate { get; set; }

        public int SalesmanCode { get; set; }

        public string? Pos { get; set; }

        public string? PcName { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscAmt { get; set; }

        public decimal RoundOff { get; set; }

        public List<SalesInvoiceItemRequest> Items { get; set; } = new();

        public List<SalesInvoiceAdditionalRequest> Additional { get; set; } = new();
    }

    public class SalesInvoiceItemRequest
    {
        public int SlNo { get; set; }

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public decimal Rate { get; set; }

        public decimal PrRate { get; set; }

        public decimal Mrp { get; set; }

        public decimal LDiscPerc { get; set; }

        public decimal LDiscPrice { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscPrice { get; set; }

        public string Batch { get; set; } = string.Empty;

        public DateTime? MfgDate { get; set; }

        public DateTime? ExpDate { get; set; }

        public int UnitCode { get; set; }

        public int GdwCode { get; set; }

        public int SordNo { get; set; }

        public int OrdRowId { get; set; }

        public string AlCode { get; set; } = string.Empty;

        public string AlName { get; set; } = string.Empty;

        public int ConvType { get; set; }

        public decimal ConvFact { get; set; }

        public decimal Taxable { get; set; }

        public int TaxCode { get; set; }

        public decimal TaxPerc { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetAmount { get; set; }

        public List<SalesInvoiceSerialRequest> SerialNumbers { get; set; } = new();
    }

    public class SalesInvoiceSerialRequest
    {
        public string InvRType { get; set; } = string.Empty;

        public int InVrNo { get; set; }

        public int InOrdNo { get; set; }

        public int InSlNo { get; set; }

        public string ItemSlNo { get; set; } = string.Empty;
    }

    public class SalesInvoiceAdditionalRequest
    {
        public int AccId { get; set; }

        public decimal Amount { get; set; }
    }
}