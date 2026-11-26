
namespace QuickBalance.API.CLS
{
    public class clsCustomer
    {
        public int Id { get; set; }
        public string? Alias { get; set; }
        public string? CusName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("isActive")]
        public bool Active { get; set; }
        public string? LogId { get; set; }
        public int AccountId { get; set; }

        public int? LedgerGroupId { get; set; }
        public string? SwiftCode { get; set; }
        public string? StateArabicName { get; set; }
        public string? AccountCode { get; set; }

        public decimal Disc { get; set; }
        public int? StateCode { get; set; }
        public string? PrintName { get; set; }
        public int CountryCode { get; set; }
        public decimal PriceLevel { get; set; }

        public string? BnkAccName { get; set; }
        public string? BnkAccNo { get; set; }
        public string? Branch { get; set; }
        public string? IbanNo { get; set; }

        public bool VatFlag { get; set; }
        public bool BillFlag { get; set; }
        public bool CrPerFlag { get; set; }
        public bool CrLimitFlag { get; set; }
        public decimal CrLimit { get; set; }

        public string? VatRegNo { get; set; }
        public string? ShipAddress { get; set; }
        public string? SecName { get; set; }
        public bool CurFlg { get; set; }

        public int? SalesManId { get; set; }
        public int CrPeriod { get; set; }
        public bool GrpItem { get; set; }
        public int ItemGroupId { get; set; }
        public bool GccFlg { get; set; }

        public int PrTmplateId { get; set; }
        public int PntSysId { get; set; }

        public string? StreetOne { get; set; }
        public string? StreetTwo { get; set; }
        public string? BuildingNo { get; set; }
        public string? PlotId { get; set; }
        public string? CityName { get; set; }
        public string? PostalCode { get; set; }
        public string? District { get; set; }

        public string? GrpVatNo { get; set; }
        public string? EinvIdType { get; set; }
        public string? EinvId { get; set; }
        public string? VatType { get; set; }

        public int PrListCd { get; set; }
        public int PrCategoryId { get; set; }
        public int PartyGrpId { get; set; }

        public string? CityNameSec { get; set; }
        public string? DistrictSec { get; set; }
        public string? AprvLst { get; set; }

        public byte[]? RowVersion { get; set; }

        public decimal OpenBal { get; set; }
        // Wire value is always uppercase CR/DR (rule 4, D12); old "Cr"/"Dr" values are normalised on read.
        private string _crDr = "DR";
        public string CrDr
        {
            get => _crDr;
            set => _crDr = string.IsNullOrWhiteSpace(value) ? "DR" : value.Trim().ToUpperInvariant();
        }
        public int CurCode { get; set; }
        public decimal CurConvRate { get; set; } = 1m;

        public List<CustomerPendingBillDto> PendingBills { get; set; } = new();
    }
    public class CustomerOpeningBalanceDto
    {
        public int Id { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int? CurrencyCode { get; set; }
        public decimal? ConversionRate { get; set; }
        public int? BranchId { get; set; }
    }

    public class CustomerPendingBillDto
    {
        public int Id { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Narration { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime? DueDate { get; set; }
        public string? DrCr { get; set; }
        public int? CurrencyCode { get; set; }
        public decimal? ConversionRate { get; set; }
        public int? BranchId { get; set; }
    }


    public class CustomerOpeningBalanceResult
    {
        public int AccountId { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public int? CurrencyCode { get; set; }
        public decimal? ConversionRate { get; set; }
    }

    public class CustomerPendingBillResult
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Narration { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime? DueDate { get; set; }
        public string? DrCr { get; set; }
        public int? CurrencyCode { get; set; }
        public decimal? ConversionRate { get; set; }
        public int? BranchId { get; set; }
    }

    public class CustomerLedgerResult
    {
        public int AccountId { get; set; }
        public int? LedgerGroupId { get; set; }
        public string? AccountCode { get; set; }
        public string? SwiftCode { get; set; }
    }

    public class CustomerAccountResult
    {
        public int? LedgerGroupId { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? ConversionRate { get; set; }
        public string? AccountId { get; set; }
        public string? SwiftCode { get; set; }
    }
}