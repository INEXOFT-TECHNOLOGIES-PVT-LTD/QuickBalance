namespace QuickBalance.API.CLS
{
    public class ClsGrpLdg
    {
        public int GroupId { get; set; }

        public string GroupName { get; set; } = "";

        public int GroupCode { get; set; }

        public string GroupNature { get; set; } = "";

        public bool SubLedgeFlag { get; set; }

        public int MGroup { get; set; }

        public bool Maingroup { get; set; }

        public int LogCd { get; set; }

        public DateTime Utime { get; set; }

        public string Order { get; set; } = "";

        public string SecName { get; set; } = "";

        public int SeqNo { get; set; }

        public string CRDR { get; set; } = "";

        public bool Gactive { get; set; }

        public string APRVLST { get; set; } = "";

        public int APRVLRBY { get; set; }

        public DateTime APRVLRDT { get; set; }

        public bool GDelFlg { get; set; }
    }
}