namespace QuickBalance.API.DTOs
{
    public class BomMasterRequest
    {
        public int Code { get; set; }

        public string BomName { get; set; } = "";

        public string SecondName { get; set; } = "";

        public string Alias { get; set; } = "";

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public int Unit { get; set; }

        public decimal Expences { get; set; }

        public string McGenFlg { get; set; } = "N";

        public int McGen { get; set; }

        public string McConsFlg { get; set; } = "N";

        public int McCons { get; set; }

        public int BranchId { get; set; }
    }
}