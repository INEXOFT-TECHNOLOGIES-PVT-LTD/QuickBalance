namespace QuickBalance.API.DTOs.Respons_DTO
{
    //public sealed class LedgerGroupDto
    //{
    //    public int GroupCode { get; set; }
    //    public string GroupName { get; set; } = string.Empty;
    //    public string? SecName { get; set; }
    //}

    public sealed class PaginatedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public long TotalRecords { get; set; }
    }

    public sealed class LedgerGroupDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public int GroupCode { get; set; }
        public string FinalAccount { get; set; } = string.Empty;
        public bool SubLedgerFlag { get; set; }
        public bool MainGroup { get; set; }
        public int MainGroupId { get; set; }
        public int SequenceNo { get; set; }
        public string CrDr { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        //public string ApprovalStatus { get; set; } = string.Empty;
        //public bool gdelflg { get; set; }
    }
}
