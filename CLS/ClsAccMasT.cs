namespace QuickBalance.API.CLS
{
    public class ClsAccMasT
    {
        public int Id { get; set; }

        public string Alias { get; set; } = "";

        public string AccName { get; set; } = "";

        public string SecName { get; set; } = "";

        public string Address { get; set; } = "";

        public string Phone { get; set; } = "";

        public bool Active { get; set; }

        public int LogCd { get; set; }

        public DateTime Uitime { get; set; }

        public string AcCode { get; set; } = "";

        public string PrintName { get; set; } = "";

        public int LedgerGroupId { get; set; }

        public decimal Depreciation { get; set; }

        public bool VatFlag { get; set; }

        public decimal VatPerc { get; set; }

        public bool TaxRetFlag { get; set; }

        public string BankAccName { get; set; } = "";

        public string BankAccno { get; set; } = "";

        public string Branch { get; set; } = "";

        public string IbanNo { get; set; } = "";

        public string SwiftCode { get; set; } = "";

        public decimal OpenBal { get; set; }

        public decimal CurConvRate { get; set; }

        public int CurCode { get; set; }

        public bool CurFlg { get; set; }

        public string APRVLST { get; set; } = "";

        public int APRVLABY { get; set; }

        public int APRVLBBY { get; set; }

        public DateTime APRVLRDT { get; set; }

        public bool DelFlg { get; set; }
    }
}