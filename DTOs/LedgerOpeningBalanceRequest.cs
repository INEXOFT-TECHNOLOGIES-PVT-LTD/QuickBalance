namespace QuickBalance.API.DTOs
{
    public class LedgerOpeningBalanceRequest
    {
        public bool All { get; set; }

        public bool SelectedGroup { get; set; }

        public bool SelectedAccount { get; set; }

        public int GroupCode { get; set; }

        public int AccountCode { get; set; }

        public bool IncludeZeroBalanceAccounts { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}