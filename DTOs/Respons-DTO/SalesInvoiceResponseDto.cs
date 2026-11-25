namespace QuickBalance.API.DTOs.Respons_DTO
{
    public sealed class SalesInvoiceResponseDto
    {
        public SalesInvoiceHeaderDto Header { get; set; } = new();
        public List<SalesInvoiceItemDto> Items { get; set; } = new();
        public List<SalesInvoiceSerialDto> SerialNumbers { get; set; } = new();
        public List<SalesInvoiceAdditionalDto> AdditionalInformations { get; set; } = new();
    }

    public sealed class SalesInvoiceHeaderDto
    {
        public int InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }
        public string? BillType { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int SalesmanCode { get; set; }
        public string? SalesmanName { get; set; }
        public int OrderNo { get; set; }

        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetValue { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal Freight { get; set; }
        public decimal RoundOff { get; set; }

        public int GodownNo { get; set; }
        public int PaymentMode { get; set; }
        public decimal CashPaid { get; set; }
        public decimal BankPaid { get; set; }

        public DateTime? DueDate { get; set; }
        public string? Remarks { get; set; }
        public string? UserName { get; set; }

        public bool Cancelled { get; set; }
        public bool Returned { get; set; }
    }

    public sealed class SalesInvoiceItemDto
    {
        public int ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Alias { get; set; }
        public string? SecondaryName { get; set; }
        public string? Bmeqn { get; set; }

        public string? UnitName { get; set; }
        public string? UnitSecondaryName { get; set; }
        public int Unit { get; set; }
        public int AltUnit { get; set; }
        public int UnitCode { get; set; }

        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal ProductRate { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalDiscount { get; set; }

        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }

        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ManufacturingDate { get; set; }

        public int GodownCode { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal ConversionQuantity { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Mrp { get; set; }

        public string? AliasCode { get; set; }
        public string? AliasName { get; set; }
        public string? ProductDescription { get; set; }
        public bool Cancelled { get; set; }
        public bool Returned { get; set; }

        public int SerialNumberCount { get; set; }
        public int OrderRowId { get; set; }
        public int TransactionRowId { get; set; }
    }

    public sealed class SalesInvoiceSerialDto
    {
        public string? InvoiceReturnType { get; set; }
        public int? InvoiceReturnNo { get; set; }
        public int? InvoiceOrderNo { get; set; }
        public int? InvoiceSerialNo { get; set; }
        public int? ProductCode { get; set; }
        public int? OutSerialNo { get; set; }
        public int? GodownCode { get; set; }
        public int? UnitCode { get; set; }
        public int? ItemSerialNo { get; set; }
    }

    public sealed class SalesInvoiceAdditionalDto
    {
        public int Code { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxAmount { get; set; }
        public int SalesLedgerCode { get; set; }
    }
}

