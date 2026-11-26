namespace QuickBalance.API.DTOs.SalesMan
{
    // =========================================================
    // SALESMAN RESPONSE
    // =========================================================
    public sealed class SalesmanResponse
    {
        public int Id { get; set; }

        public int? EmployeeCode { get; set; }

        public string SalesmanName { get; set; } = string.Empty;

        public string? SecondName { get; set; } = string.Empty;

        public string? PrintName { get; set; } = string.Empty;

        public string? Address { get; set; } = string.Empty;

        public string? Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int SalesmanGroupId { get; set; }

        public bool IsActive { get; set; }

        public bool ConnectWithAccount { get; set; }

        public int AccountId { get; set; }

        //public int LogCode { get; set; }

        //public DateTime? UpdateTime { get; set; }
    }

    // =========================================================
    // SALESMAN SAVE RESPONSE
    // =========================================================
    public sealed class SalesmanSaveResponse
    {
        public int Id { get; set; }

        public int SalesmanCode { get; set; }

        public int EmployeeCode { get; set; }
    }

    // =========================================================
    // SALESMAN LOOKUP RESPONSE
    // =========================================================
    public sealed class SalesmanLookupResponse
    {
        public int Id { get; set; }

        public string Salesman { get; set; } = string.Empty;
    }

    // =========================================================
    // ACTIVE SALESMAN RESPONSE
    // =========================================================
    public sealed class ActiveSalesmanResponse
    {
        public int Id { get; set; }

        public int SalesmanCode { get; set; }

        public string SalesmanName { get; set; } = string.Empty;
    }

    // =========================================================
    // ACCOUNT VALIDATION RESPONSE
    // =========================================================
    public sealed class SalesmanAccountValidationResponse
    {
        public bool Exists { get; set; }

        public IReadOnlyList<SalesmanResponse> Salesmen { get; set; }
            = Array.Empty<SalesmanResponse>();
    }
}
