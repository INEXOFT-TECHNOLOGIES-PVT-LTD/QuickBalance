namespace QuickBalance.API.DTOs.Respons_DTO
{
    public sealed class ItemGroupResponse
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int GroupCode { get; set; }

        public bool IsPrimaryGroup { get; set; }
        public int PrimaryGroupId { get; set; }

        public bool TagWithAccount { get; set; }
        public int StockAccountId { get; set; }
        public int PurchaseAccountId { get; set; }
        public int SalesAccountId { get; set; }

        public int LogCode { get; set; }
        public DateTime? UpdateTime { get; set; }
        public bool IsActive { get; set; }
        public string? SecName { get; set; }
    }

    /// <summary>Row for GET /api/v1/item-group (list only; no internal fields).</summary>
    public sealed class ItemGroupListItemDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int GroupCode { get; set; }

        public bool IsPrimaryGroup { get; set; }
        public int PrimaryGroupId { get; set; }

        public bool TagWithAccount { get; set; }
        public int StockAccountId { get; set; }
        public int PurchaseAccountId { get; set; }
        public int SalesAccountId { get; set; }

        public bool IsActive { get; set; }
        public string? SecName { get; set; }
    }

    public sealed class ItemGroupListResponse
    {
        public IReadOnlyList<ItemGroupResponse> Data { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}
