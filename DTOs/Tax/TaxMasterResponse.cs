namespace QuickBalance.API.DTOs.Tax
{

    public sealed class TaxMasterResponse
    {
        public int Id { get; set; }

        public string TaxName { get; set; } = string.Empty;

        public decimal TaxPercentage { get; set; }

        public bool IsActive { get; set; }

        //public int UserId { get; set; }

        public int PurchaseAccountId { get; set; }

        public int PurchaseReturnAccountId { get; set; }

        public int SalesAccountId { get; set; }

        public int SalesReturnAccountId { get; set; }

        //public DateTime? UpdatedAt { get; set; }

        public int VatAccountId { get; set; }

        public int TaxAccountId { get; set; }

        public string SecondName { get; set; } = string.Empty;

        public string TaxType { get; set; } = "T";
    }


    public sealed class TaxLookupResponse
    {
        public int Id { get; set; }

        public string TaxName { get; set; } = string.Empty;
    }
    public sealed class TaxPercentageResponse
    {
        public decimal TaxPercentage { get; set; }
    }
}
