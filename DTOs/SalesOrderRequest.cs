namespace QuickBalance.API.DTOs
{
    public class SalesOrderRequest
    {
        public int SordNo { get; set; }

        public DateTime Date { get; set; }

        public int CustomerCode { get; set; }

        public int SalesmanCode { get; set; }

        public decimal DiscPerc { get; set; }  //additional discount percentage

        public decimal DiscAmt { get; set; } //additional discount amount

        public decimal RoundOff { get; set; } //round off amount

        public List<SalesOrderItemRequest> Items { get; set; } = new();

        public List<SalesOrderAdditionalRequest> Additional { get; set; } = new();
    }

    public class SalesOrderItemRequest
    {
        public int SlNo { get; set; }

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public decimal Rate { get; set; }

        public decimal LDiscPerc { get; set; }

        public decimal LDiscPrice { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal DiscPrice { get; set; }

        public int UnitCode { get; set; }

        public int GdwCode { get; set; }

        public string AlCode { get; set; } = string.Empty;

        public string AlName { get; set; } = string.Empty;

        public int ConvType { get; set; }

        public decimal ConvFact { get; set; }

        public decimal Taxable { get; set; }

        public int TaxCode { get; set; }

        public decimal TaxPerc { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetAmount { get; set; }
    }

    public class SalesOrderAdditionalRequest
    {
        public int AccId { get; set; }

        public decimal Amount { get; set; }
    
    }
}