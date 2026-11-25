namespace QuickBalance.API.DTOs.Respons_DTO
{
    public sealed class CustomerLedgerGroupDto
    {
        public int GroupId { get; set; }

        public string GroupCode { get; set; } = string.Empty;
    }


    public sealed class SupplierLedgerGroupDto
    {
        public int GroupId { get; set; }

        public string GroupCode { get; set; } = string.Empty;
    }
}

