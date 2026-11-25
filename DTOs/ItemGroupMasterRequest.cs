namespace QuickBalance.API.DTOs
{
    public class ItemGroupMasterRequest
    {
        public int GroupId { get; set; }

        public string GroupName { get; set; } = "";

        public string SecondName { get; set; } = "";

        public int GroupCode { get; set; }

        public string IsPrimaryGroup { get; set; } = "N";

        public int PrimaryGroupId { get; set; }

        public string TagWithAccount { get; set; } = "N";

        public int StockAccount { get; set; }

        public int PurchaseAccount { get; set; }

        public int SalesAccount { get; set; }

        public string IsActive { get; set; } = "Y";
    }
}