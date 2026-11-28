
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs

{



    using System.ComponentModel.DataAnnotations;

    public class CustomerRequest
    {
        [StringLength(25, ErrorMessage = "Alias cannot exceed 25 characters.")]
        public string? Alias { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string? CusName { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(25, ErrorMessage = "Phone cannot exceed 25 characters.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string? Email { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("isActive")]
        public bool Active { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Ledger group ID must be greater than zero.")]
        public int? LedgerGroupId { get; set; }

        [Required(ErrorMessage = "Please select a valid region.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid region.")]
        public int? StateCode { get; set; }

        [StringLength(100, ErrorMessage = "Print name cannot exceed 100 characters.")]
        public string? PrintName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid country.")]
        public int CountryCode { get; set; }

        [StringLength(100, ErrorMessage = "Bank account name cannot exceed 100 characters.")]
        public string? BnkAccName { get; set; }

        [StringLength(25, ErrorMessage = "Bank account number cannot exceed 25 characters.")]
        public string? BnkAccNo { get; set; }

        [StringLength(100, ErrorMessage = "Branch cannot exceed 100 characters.")]
        public string? Branch { get; set; }

        [StringLength(34, ErrorMessage = "IBAN number cannot exceed 34 characters.")]
        public string? IbanNo { get; set; }

        public bool VatFlag { get; set; }
        public bool BillFlag { get; set; }
        public bool CrPerFlag { get; set; }
        public bool CrLimitFlag { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "Credit limit cannot be negative.")]
        [Range(0, 1000000, ErrorMessage = "Credit limit must be between 0 and 1000000.")]

        public decimal CrLimit { get; set; }

        [RegularExpression(@"^[0-9]{15}$",
            ErrorMessage = "VAT number must contain exactly 15 digits.")]
        public string? VatRegNo { get; set; }

        [StringLength(250, ErrorMessage = "Shipping address cannot exceed 250 characters.")]
        public string? ShipAddress { get; set; }

        [StringLength(100, ErrorMessage = "Secondary name cannot exceed 100 characters.")]
        public string? SecName { get; set; }

        public bool CurFlg { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Salesman ID must be greater than zero.")]
        [System.Text.Json.Serialization.JsonPropertyName("salesManId")]
        public int? SalesMan { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "Credit period cannot be negative.")]
        [Range(0, 100, ErrorMessage = "Credit period must be between 0 and 100.")]

        public int CrPeriod { get; set; }

        public bool GccFlg { get; set; }

        [StringLength(100, ErrorMessage = "Street one cannot exceed 100 characters.")]
        public string? StreetOne { get; set; }

        [StringLength(100, ErrorMessage = "Street two cannot exceed 100 characters.")]
        public string? StreetTwo { get; set; }

        [StringLength(25, ErrorMessage = "Building number cannot exceed 25 characters.")]
        public string? BuildingNo { get; set; }

        [StringLength(25, ErrorMessage = "Plot ID cannot exceed 25 characters.")]
        public string? PlotId { get; set; }

        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters.")]
        public string? CityName { get; set; }

        [StringLength(15, ErrorMessage = "Postal code cannot exceed 15 characters.")]
        public string? PostalCode { get; set; }

        [StringLength(100, ErrorMessage = "District cannot exceed 100 characters.")]
        public string? District { get; set; }

        [StringLength(25, ErrorMessage = "Group VAT number cannot exceed 25 characters.")]
        public string? GrpVatNo { get; set; }

        [StringLength(25, ErrorMessage = "E-invoice ID type cannot exceed 25 characters.")]
        public string? EinvIdType { get; set; }

        [StringLength(50, ErrorMessage = "E-invoice ID cannot exceed 50 characters.")]
        public string? EinvId { get; set; }

        [StringLength(25, ErrorMessage = "VAT type cannot exceed 25 characters.")]
        public string? VatType { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "Party group ID cannot be negative.")]
        [Range(0, 100000, ErrorMessage = "Party group ID must be between 0 and 100000.")]
        public int PartyGrpId { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "Opening balance cannot be negative.")]
        [Range(0, 1000000, ErrorMessage = "Opening balance must be between 0 and 1000000.")]

        public decimal OpenBal { get; set; }

        //[Required(ErrorMessage = "Debit/Credit type is required.")]
        //[RegularExpression(@"^(DR|CR)$",
        //    ErrorMessage = "Debit/Credit type must be DR or CR.")]

        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:DR|CR)$",
    ErrorMessage = "Debit/Credit type must be DR or CR.")]
        public string CrDr
        {
            get => _crDr;
            set => _crDr = string.IsNullOrWhiteSpace(value) ? value! : value.Trim().ToUpperInvariant();
        }
        private string _crDr = "DR";

        //[Range(0, int.MaxValue, ErrorMessage = "Currency code cannot be negative.")]
        [Range(0, 100000, ErrorMessage = "Currency code must be between 0 and 100000.")]

        public int CurCode { get; set; }

        //[Range(typeof(decimal), "0.000001", "999999999999",
        //    ErrorMessage = "Conversion rate must be greater than zero.")]
        [Range(typeof(decimal), "0.000001", "100",
    ErrorMessage = "Conversion rate must be between 0.000001 and 100.")]
        public decimal CurConvRate { get; set; } = 1m;

        [StringLength(25, ErrorMessage = "SWIFT code cannot exceed 25 characters.")]
        public string? SwiftCode { get; set; }

        public List<CustomerPendingBillRequest>? PendingBills { get; set; } = new();
    }

    public class CustomerUpdateRequest : CustomerRequest
    {
        [Required(ErrorMessage = "RowVersion is required.")]
        public byte[]? RowVersion { get; set; }
    }
    //    public class CustomerRequest
    //    {
    //        public string? Alias { get; set; }
    //        public string? CusName { get; set; }
    //        public string? Address { get; set; }
    //        public string? Phone { get; set; }
    //        public string? Email { get; set; }
    //        public bool Active { get; set; } = true;
    //        //public string? LogCd { get; set; }
    //        public int? LedgerGroupId { get; set; }
    //        //public decimal Disc { get; set; }
    //        public int StateCode { get; set; }
    //        public string? PrintName { get; set; }
    //        public int CountryCode { get; set; }
    //        //public decimal PriceLevel { get; set; }
    //        public string? BnkAccName { get; set; }
    //        public string? BnkAccNo { get; set; }
    //        public string? Branch { get; set; }
    //        public string? IbanNo { get; set; }
    //        public bool VatFlag { get; set; }
    //        public bool BillFlag { get; set; }
    //        public bool CrPerFlag { get; set; }
    //        public bool CrLimitFlag { get; set; }
    //        public decimal CrLimit { get; set; }
    //        //public DateTime? UiTime { get; set; }

    //        [RegularExpression(@"^[0-9]{15}$",
    //        ErrorMessage = "VAT number must contain exactly 15 digits.")]
    //        public string? VatRegNo { get; set; }
    //        public string? ShipAddress { get; set; }
    //        public string? SecName { get; set; }
    //        public bool CurFlg { get; set; }
    //        public int? SalesMan { get; set; }
    //        public int CrPeriod { get; set; }
    //        //public bool GrpItem { get; set; }
    //        //public int ItemGroupCD { get; set; }
    //        public bool GccFlg { get; set; }
    //        //public bool DelFlg { get; set; }
    //        //public string? PrTmpCode { get; set; }
    //        //public string? PntSysId { get; set; }
    //        public string? StreetOne { get; set; }
    //        public string? StreetTwo { get; set; }
    //        public string? BuildingNo { get; set; }
    //        public string? PlotId { get; set; }
    //        public string? CityName { get; set; }
    //        public string? PostalCode { get; set; }
    //        public string? District { get; set; }
    //        public string? GrpVatNo { get; set; }
    //        public string? EinvIdType { get; set; }
    //        public string? EinvId { get; set; }
    //        public string? VatType { get; set; }
    //        //public string? PrListCd { get; set; }
    //        //public string? PrCatCd { get; set; }
    //        public int PartyGrpId { get; set; }
    //        //public string? CityNameSec { get; set; }
    //        //public string? DistrictSec { get; set; }  
    //        //public string? AprvLst { get; set; }
    //        public decimal OpenBal { get; set; }
    //        public string CrDr { get; set; } = "Cr";
    //        public int CurCode { get; set; }
    //        public decimal CurConvRate { get; set; } = 1m;

    //        public  string? SwiftCode { get; set; }

    //        public List<CustomerPendingBillRequest>? PendingBills { get; set; } = new();

    //    }

    //    public class CustomerUpdateRequest : CustomerRequest
    //    {
    //        public byte[]? RowVersion { get; set; }

    //    }


    //public class CustomerOpeningBalanceRequest
    //{
    //    public decimal Amount { get; set; }

    //    // DR = Debit, CR = Credit
    //    public string DrCr { get; set; } = "DR";

    //    public int CurrencyCode { get; set; }

    //    public decimal ConversionRate { get; set; } = 1m;

    //    public int BranchId { get; set; }
    //}
    //public class CustomerPendingBillRequest
    //{
    //    public DateTime InvoiceDate { get; set; }

    //    public string? Narration { get; set; }

    //    public string InvoiceNumber { get; set; } = string.Empty;

    //    public decimal Amount { get; set; }

    //    public DateTime DueDate { get; set; }

    //    // DR = Debit, CR = Credit
    //    public string DrCr { get; set; } = "DR";
    //}


    public class CustomerOpeningBalanceRequest
    {
        // Existing row ID; null or 0 means create a new row.
        [Range(1, int.MaxValue,
            ErrorMessage = "Opening balance ID must be greater than zero.")]
        public int? Id { get; set; }

        [Range(typeof(decimal), "0", "1000000",
            ErrorMessage = "Opening balance amount must be between 0 and 1000000.")]
        public decimal Amount { get; set; }

        // DR = Debit, CR = Credit
        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:DR|CR)$",
            ErrorMessage = "Debit/Credit type must be DR or CR.")]
        public string DrCr { get; set; } = "DR";

        [Range(0, 100000,
            ErrorMessage = "Currency code must be between 0 and 100000.")]
        public int CurrencyCode { get; set; }

        [Range(typeof(decimal), "0.000001", "100",
            ErrorMessage = "Conversion rate must be between 0.000001 and 100.")]
        public decimal ConversionRate { get; set; } = 1m;

        [Range(1, 100000,
            ErrorMessage = "Branch ID must be between 1 and 100000.")]
        public int BranchId { get; set; }
    }

    public class CustomerPendingBillRequest
    {
        // Existing bill ID; null or 0 means create a new bill.
        [Range(1, int.MaxValue,
            ErrorMessage = "Pending bill ID must be greater than zero.")]
        public int? Id { get; set; }

        [Required(ErrorMessage = "Invoice date is required.")]
        public DateTime? InvoiceDate { get; set; }

        [StringLength(250,
            ErrorMessage = "Narration cannot exceed 250 characters.")]
        public string? Narration { get; set; }

        [Required(ErrorMessage = "Invoice number is required.")]
        [StringLength(50,
            ErrorMessage = "Invoice number cannot exceed 50 characters.")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "1000000",
            ErrorMessage = "Pending bill amount must be between 0 and 1000000.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime? DueDate { get; set; }

        // DR = Debit, CR = Credit
        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:DR|CR)$",
            ErrorMessage = "Debit/Credit type must be DR or CR.")]
        public string DrCr { get; set; } = "DR";
    }
}