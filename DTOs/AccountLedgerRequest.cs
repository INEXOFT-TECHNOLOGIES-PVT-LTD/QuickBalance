using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class AccountLedgerRequest
    //{
    //    public string Alias { get; set; } = "";

    //    public string AccountName { get; set; } = "";

    //    public string SecondName { get; set; } = "";

    //    public string Address { get; set; } = "";

    //    public string Phone { get; set; } = "";

    //    public string AccountCode { get; set; } = "";

    //    public string PrintName { get; set; } = "";

    //    public int GroupId { get; set; }

    //    public decimal Depreciation { get; set; } = 0;

    //    public bool VatFlag { get; set; } = false;

    //    public decimal VatPercentage { get; set; } = 0;

    //    public bool TaxReturnFlag { get; set; } = false;

    //    public string BankAccountName { get; set; } = "";

    //    public string BankAccountNumber { get; set; } = "";

    //    public string Branch { get; set; } = "";

    //    public string IbanNumber { get; set; } = "";

    //    public string SwiftCode { get; set; } = "";

    //    public decimal OpeningBalance { get; set; } = 0;

    //    public string OpeningBalanceType { get; set; } = "CR";

    //    public bool CurrencyFlag { get; set; } = false;

    //    public int CurrencyCode { get; set; } = 0;

    //    public decimal CurrencyConversionRate { get; set; } = 1;

    //    public bool IsActive { get; set; } = true;
    //}


    public class AccountLedgerRequest : IValidatableObject
    {
        [StringLength(100, ErrorMessage = "Alias cannot exceed 100 characters.")]
        public string? Alias { get; set; } = "";

        [Required(ErrorMessage = "Account name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Account name must be between 2 and 100 characters.")]
        public string AccountName { get; set; } = "";

        [StringLength(100, ErrorMessage = "Second name cannot exceed 100 characters.")]
        public string? SecondName { get; set; } = "";

        [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters.")]
        public string? Address { get; set; }

        public string? Phone { get; set; }

        //public string AccountCode { get; set; } = "";

        public string? PrintName { get; set; }

        [Required(ErrorMessage = "Please select a valid account group.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid account group.")]
        public int GroupId { get; set; }

        [Range(typeof(decimal), "0", "1000",
            ErrorMessage = "Depreciation must be between 0 and 1000.")]
        public decimal? Depreciation { get; set; }

        public bool VatFlag { get; set; } = false;

        [Range(typeof(decimal), "0", "100",
            ErrorMessage = "VAT percentage must be between 0 and 100.")]
        public decimal? VatPercentage { get; set; }

        public bool TaxReturnFlag { get; set; } = false;

        public string? BankAccountName { get; set; }

        public string? BankAccountNumber { get; set; }

        public string? Branch { get; set; }

        public string? IbanNumber { get; set; }

        public string? SwiftCode { get; set; }

        [Range(typeof(decimal), "0", "10000000",
            ErrorMessage = "Opening balance must be between 0 and 10,000,000.")]
        public decimal? OpeningBalance { get; set; }

        [Required(ErrorMessage = "Opening balance type is required.")]
        [RegularExpression("^(CR|DR)$",
            ErrorMessage = "Opening balance type must be either CR or DR.")]
        public string? OpeningBalanceType { get; set; } = "CR";

        public bool CurrencyFlag { get; set; } = false;

        public int? CurrencyCode { get; set; }

        // Validated in Validate() only when CurrencyFlag is true.
        public decimal? CurrencyConversionRate { get; set; }

        public bool Active { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (CurrencyFlag && (CurrencyCode ?? 0) <= 0)
            {
                yield return new ValidationResult(
                    "Please select a valid currency.",
                    new[] { nameof(CurrencyCode) });
            }

            if (CurrencyFlag && (CurrencyConversionRate ?? 0) <= 0)
            {
                yield return new ValidationResult(
                    "Currency conversion rate must be greater than zero.",
                    new[] { nameof(CurrencyConversionRate) });
            }

            if (VatFlag && (VatPercentage ?? 0) <= 0)
            {
                yield return new ValidationResult(
                    "VAT percentage must be greater than zero when VAT is enabled.",
                    new[] { nameof(VatPercentage) });
            }
        }

        public class AccountLedgerUpdateRequest : AccountLedgerRequest
        {
            public byte[]? RowVersion { get; set; }
        }
    }
}