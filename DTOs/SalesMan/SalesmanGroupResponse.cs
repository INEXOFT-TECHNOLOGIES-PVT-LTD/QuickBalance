namespace QuickBalance.API.DTOs.SalesMan
{
    public sealed class SalesmanGroupResponse
    {
        public int Id { get; set; }

        public string GroupName { get; set; } = string.Empty;

        public string SecondName { get; set; } = string.Empty;

        public string PrintName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int LogCode { get; set; }

        public DateTime? UpdateTime { get; set; }
    }

    public sealed class SalesmanGroupLookupResponse
    {
        public int Id { get; set; }

        public string GroupName { get; set; } = string.Empty;
    }
}
