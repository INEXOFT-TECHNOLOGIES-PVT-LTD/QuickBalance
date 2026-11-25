namespace QuickBalance.API.DTOs
{
    public class TaxMasterRequest
    {
        public string TaxName { get; set; } = "";

        public string SecondName { get; set; } = "";

        public decimal TaxPercentage { get; set; }

        public bool IsActive { get; set; } = true;

        public int SalesAccountId { get; set; }

        public int SalesReturnAccountId { get; set; }

        public int PurchaseAccountId { get; set; }

        public int PurchaseReturnAccountId { get; set; }

        public int VatAccountId { get; set; }

        public int TaxAccountId { get; set; }

        public string TaxType { get; set; } = "T";
    }
}