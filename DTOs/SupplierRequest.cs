
using QuickBalance.API.DTOs.Respons_DTO;
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{


    using System.ComponentModel.DataAnnotations;

    public class SupplierRequest
    {
        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(50, ErrorMessage = "Supplier name cannot exceed 50 characters.")]
        public string? SupName { get; set; }

        [StringLength(25, ErrorMessage = "Alias cannot exceed 25 characters.")]
        public string? Alias { get; set; }

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

        // Omitted or 0 -> default supplier group (27). Positive values are validated against the shared rule.
        [Range(0, int.MaxValue, ErrorMessage = "Ledger group ID cannot be negative.")]
        public int? LedgerGroupId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid region.")]
        public int StateCode { get; set; }

        [StringLength(100, ErrorMessage = "Print name cannot exceed 100 characters.")]
        public string? PrintName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid country.")]
        public int CountryCode { get; set; }

        // Bank details
        [StringLength(100, ErrorMessage = "Bank account name cannot exceed 100 characters.")]
        public string? BnkAccName { get; set; }

        [StringLength(25, ErrorMessage = "Bank account number cannot exceed 25 characters.")]
        public string? BnkAccNo { get; set; }

        [StringLength(100, ErrorMessage = "Branch cannot exceed 100 characters.")]
        public string? Branch { get; set; }

        [StringLength(34, ErrorMessage = "IBAN cannot exceed 34 characters.")]
        public string? IbanNo { get; set; }

        [StringLength(25, ErrorMessage = "SWIFT code cannot exceed 25 characters.")]
        public string? SwiftCode { get; set; }

        // VAT and billing
        public bool VatFlag { get; set; }
        public bool BillFlag { get; set; }  
        public bool CrPerFlag { get; set; }
        public bool CrLimitFlag { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "Credit limit cannot be negative.")]
        [Range(typeof(decimal), "0", "1000000",
    ErrorMessage = "Credit limit must be between 0 and 1000000.")]
        public decimal CrLimit { get; set; }

        [RegularExpression(@"^[0-9]{15}$",
            ErrorMessage = "VAT number must contain exactly 15 digits.")]
        public string? VatRegNo { get; set; }

        // Address and credit
        [StringLength(250, ErrorMessage = "Shipping address cannot exceed 250 characters.")]
        public string? ShipAddress { get; set; }

        [StringLength(100, ErrorMessage = "Secondary name cannot exceed 100 characters.")]
        public string? SecName { get; set; }

        public bool CurFlg { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "Credit period cannot be negative.")]

        [Range(0, 100,
            ErrorMessage = "Credit period must be between 0 and 100.")]
        public int CrPeriod { get; set; }

        public bool GccFlg { get; set; }

        // E-invoice address details
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

        [Range(0, int.MaxValue, ErrorMessage = "Party group ID cannot be negative.")]
        public int PartyGrpId { get; set; }

        //[Range(0, double.MaxValue, ErrorMessage = "Opening balance cannot be negative.")]
        [Range(typeof(decimal), "0", "1000000",
        ErrorMessage = "Opening balance must be between 0 and 1000000.")]
        public decimal OpenBal { get; set; }

        [Required(ErrorMessage = "Debit/Credit type is required.")]
        [RegularExpression(@"^(?i:CR|DR)$",
            ErrorMessage = "Credit or Debit must be either CR or DR.")]
        public string CrDr
        {
            get => _crDr;
            set => _crDr = string.IsNullOrWhiteSpace(value) ? value! : value.Trim().ToUpperInvariant();
        }
        private string _crDr = "CR";

        //[Range(0, int.MaxValue, ErrorMessage = "Currency code cannot be negative.")]
        [Range(0, 100,
    ErrorMessage = "Currency code must be between 0 and 100.")]
        public int CurCode { get; set; }

        //[Range(typeof(decimal), "0.000001", "999999999999",
        //    ErrorMessage = "Conversion rate must be greater than zero.")]

        [Range(typeof(decimal), "0.000001", "100",
    ErrorMessage = "Conversion rate must be between 0.000001 and 100.")]
        public decimal CurConvRate { get; set; } = 1m;
        public List<PendingBillRequest>? PendingBills { get; set; } = new();


        //public List<CustomerPendingBillRequest>? PendingBills { get; set; }

        //public List<PendingBillRequest>? PendingBills { get; set; }
    }

    public class SupplierEditRequest : SupplierRequest
    {
        [Required(ErrorMessage = "RowVersion is required.")]
        public byte[]? RowVersion { get; set; }
    }



    public class SupplierRequestUpdatedDto
    {
        public string? Alias { get; set; }
        public string? SupName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool Active { get; set; }
        public int Logcd { get; set; }
        public int AccId { get; set; }

        public int? LedgerGroupId { get; set; }

        public decimal Disc { get; set; }
        public int StateCode { get; set; }
        public string? PrintName { get; set; }
        public int CountryCode { get; set; }
        public decimal PriceLevel { get; set; }

        // Bank details
        public string? BnkAccName { get; set; }
        public string? BnkAccNo { get; set; }
        public string? Branch { get; set; }
        public string? IbanNo { get; set; }
        public string? SwiftCode { get; set; }

        // VAT and billing
        public bool VatFlag { get; set; }
        public bool BillFlag { get; set; }
        public bool CrPerFlag { get; set; }
        public bool CrLimitFlag { get; set; }
        public decimal CrLimit { get; set; }

        [RegularExpression(@"^[0-9]{15}$",
        ErrorMessage = "VAT number must contain exactly 15 digits.")]
        public string? VatRegNo { get; set; }
        //public string? VatRegNo { get; set; }

        // Address and credit
        public string? ShipAddress { get; set; }
        public string? SecName { get; set; }
        public bool CurFlg { get; set; }
        public int CrPeriod { get; set; }
        public bool GrpItem { get; set; }
        public int ItemGroupCD { get; set; }
        public bool GccFlg { get; set; }

        // E-invoice address details
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

        // Supplier group
        public int PartyGrpId { get; set; }

        // Secondary address
        public string? CityNameSec { get; set; }
        public string? DistrictSec { get; set; }

        // Approval
        public string? Aprvlst { get; set; }
        public byte[]? RowVersion { get; set; }

        public decimal OpenBal { get; set; }
        public string CrDr { get; set; } = "Cr";
        public int CurCode { get; set; }
        public decimal CurConvRate { get; set; } = 1m;
        public List<PendingBillRequest>? PendingBills { get; set; }

    }
}
//namespace QuickBalance.API.DTOs
//{
//    public class SupplierRequest
//    {
//        public string? Alias { get; set; }
//        public string? SupName { get; set; }
//        public string? Address { get; set; }
//        public string? Phone { get; set; }
//        public string? Email { get; set; }
//        public bool Active { get; set; }
//        public int Logcd { get; set; }
//        public int LedgerGroupId { get; set; }
//        public decimal Disc { get; set; }
//        public int StateCode { get; set; }
//        public string? PrintName { get; set; }
//        public int CountryCode { get; set; }
//        public decimal PriceLevel { get; set; }

//        // Bank details
//        public string? BnkAccName { get; set; }
//        public string? BnkAccNo { get; set; }
//        public string? Branch { get; set; }
//        public string? IbanNo { get; set; }
//        public string? SwiftCode { get; set; }

//        // VAT and billing
//        public bool VatFlag { get; set; }
//        public bool BillFlag { get; set; }
//        public bool CrPerFlag { get; set; }
//        public bool CrLimitFlag { get; set; }
//        public decimal CrLimit { get; set; }
//        public string? VatRegNo { get; set; }

//        // Address and credit
//        public string? ShipAddress { get; set; }
//        public string? SecName { get; set; }
//        public bool CurFlg { get; set; }
//        public int CrPeriod { get; set; }
//        public bool GrpItem { get; set; }
//        public int ItemGroupCD { get; set; }
//        public bool GccFlg { get; set; }

//        // E-invoice address details
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

//        // Supplier group
//        public int PartyGrpId { get; set; }

//        // Secondary address
//        public string? CityNameSec { get; set; }
//        public string? DistrictSec { get; set; }

//        // Approval
//        public string? Aprvlst { get; set; }

//    }
   