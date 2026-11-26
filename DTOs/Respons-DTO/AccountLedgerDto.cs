using System.Text.Json.Serialization;

namespace QuickBalance.API.DTOs.Respons_DTO
{

    // Wire names match AccountLedgerRequest. C# property names stay as they are
    // because Dapper maps the SQL aliases to them.
    public class AccountLedgerDtos
    {
        public int Id { get; set; }

        public string? Alias { get; set; }

        [JsonPropertyName("accountName")]
        public string? AccName { get; set; }

        [JsonIgnore]
        public string? AcCode { get; set; }

        /// <summary>Read-only. ACCMAS.ACCODE under the name the UI and the customer API use.</summary>
        public string? AccountCode => AcCode;

        public string? PrintName { get; set; }

        [JsonPropertyName("secondName")]
        public string? SecName { get; set; }

        [JsonPropertyName("groupId")]
        public int? LedgerGroupId { get; set; }

        public string? Address { get; set; }
        public string? Phone { get; set; }

        [JsonIgnore]
        public bool? Active { get; set; }

        /// <summary>Read-only. A NULL ACTIVE is returned as true.</summary>
        [JsonPropertyName("isActive")]
        public bool? IsActive => Active ?? true;
        //public bool? DelFlg { get; set; }

        /// <summary>Signed, as stored: debit is negative. Not serialised; see OpeningBalance.</summary>
        [JsonIgnore]
        public decimal? OpenBal { get; set; }

        /// <summary>Read-only. Absolute value of the stored opening balance.</summary>
        [JsonPropertyName("openingBalance")]
        public decimal OpeningBalance => Math.Abs(OpenBal ?? 0m);

        /// <summary>Read-only. "DR" when the stored opening balance is negative, otherwise "CR".</summary>
        [JsonPropertyName("openingBalanceType")]
        public string OpeningBalanceType => (OpenBal ?? 0m) < 0m ? "DR" : "CR";

        public decimal? Depreciation { get; set; }

        public bool? VatFlag { get; set; }

        [JsonPropertyName("vatPercentage")]
        public decimal? VatPerc { get; set; }

        [JsonPropertyName("taxReturnFlag")]
        public bool? TaxRetFlag { get; set; }

        [JsonPropertyName("bankAccountName")]
        public string? BankAccName { get; set; }

        [JsonPropertyName("bankAccountNumber")]
        public string? BankAccNo { get; set; }

        [JsonPropertyName("ibanNumber")]
        public string? IbanNo { get; set; }

        public string? SwiftCode { get; set; }
        public string? Branch { get; set; }

        [JsonIgnore]
        public bool? CurFlg { get; set; }

        /// <summary>Read-only. A NULL CURFLG is returned as false.</summary>
        [JsonPropertyName("currencyFlag")]
        public bool CurrencyFlag => CurFlg ?? false;

        [JsonPropertyName("currencyConversionRate")]
        public decimal? CurConvRate { get; set; }

        [JsonPropertyName("currencyCode")]
        public int? CurCode { get; set; }

        //public int? CreatedBy { get; set; }
        //public DateTime? UiTime { get; set; }

        public byte[]? RowVersion { get; set; }
    }

    public class AccountLedgerDto
        {
            public int Id { get; set; }
            public string? AccName { get; set; }
            public string? Alias { get; set; }
            public string? AcCode { get; set; }
            public string? SecName { get; set; }
            public int? MGroup { get; set; }
            public decimal? OpenBal { get; set; }
            public bool? Active { get; set; }

            public bool? CurFlg { get; set; }
            public string? Address { get; set; }
            public string? ShipAddress { get; set; }
            public string? Phone { get; set; }
            public string? VatRegNo { get; set; }
            public bool? VatFlag { get; set; }
            public string? GrpItem { get; set; }
            public int? ItemGrpCd { get; set; }
            public string? CrPeriod { get; set; }
            public bool? CrLimitFlag { get; set; }
            public string? CrLimit { get; set; }
            public string? Salesman { get; set; }
            public string? CountryCode { get; set; }
            public bool? GccFlg { get; set; }
            public string? PrintName { get; set; }

            public decimal? Depreciation { get; set; }
            public decimal? VatPerc { get; set; }
            public bool? TaxRetFlag { get; set; }

            public string? BankAccName { get; set; }
            public string? BankAccNo { get; set; }
            public string? IbanNo { get; set; }
            public string? SwiftCode { get; set; }
            public string? Branch { get; set; }

            public decimal? CurConvRate { get; set; }
            public int? CurCode { get; set; }

            public decimal? OpnVal { get; set; }
        }

}

