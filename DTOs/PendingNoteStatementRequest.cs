namespace QuickBalance.API.DTOs
{
    public class PendingNoteStatementRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string VrType { get; set; } = "GRN";

        public bool Pending { get; set; }
        public bool AccWise { get; set; }
        public int PartyCd { get; set; }

        public bool SmWise { get; set; }
        public int SmCode { get; set; }

        public bool Summary { get; set; }

        public bool IncludeUnapproved { get; set; }

        public int CompCode { get; set; }
        public int UserId { get; set; }

        public string VoucherTypeCodes { get; set; } = "";

        public string ServerName { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }
}