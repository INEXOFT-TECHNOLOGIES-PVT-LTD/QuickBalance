namespace QuickBalance.API.DTOs.Respons_DTO
{

    public class AccountLedgerDtos
    {
        public int Id { get; set; }

        public string? Alias { get; set; }
        public string? AccName { get; set; }
        public string? AcCode { get; set; }

        /// <summary>Alias of AcCode (ACCMAS.ACCODE) using the name the UI and the customer API use.</summary>
        public string? AccountCode => AcCode;

        public string? PrintName { get; set; }
        public string? SecName { get; set; }

        public int? LedgerGroupId { get; set; }

        public string? Address { get; set; }
        public string? Phone { get; set; }

        public bool? Active { get; set; }
        //public bool? DelFlg { get; set; }

        public decimal? OpenBal { get; set; }

        public decimal? Depreciation { get; set; }

        public bool? VatFlag { get; set; }
        public decimal? VatPerc { get; set; }
        public bool? TaxRetFlag { get; set; }

        public string? BankAccName { get; set; }
        public string? BankAccNo { get; set; }
        public string? IbanNo { get; set; }
        public string? SwiftCode { get; set; }
        public string? Branch { get; set; }

        public bool? CurFlg { get; set; }
        public decimal? CurConvRate { get; set; }
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

