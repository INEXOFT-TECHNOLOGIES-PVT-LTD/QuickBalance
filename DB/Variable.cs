namespace QuickBalance.API.DB
{
    public class Variable
    {
        public string format = "dd-MMM-yyyy";
        public string TwoDecString = "#,0.00";

        // Financial Year
        public DateTime fyStartDate { get; set; }
        public DateTime fyEndDate { get; set; }
        public string Finyr { get; set; } = string.Empty;
        public string PrevFINYR { get; set; } = string.Empty;
        public DateTime PrevStartDate { get; set; }
        public DateTime PrevEndDate { get; set; }

        // Formatting and General Settings
        public string VatLbl { get; set; } = string.Empty;
        public int DecimalPoint { get; set; }
        public string DecString { get; set; } = string.Empty;
        public int QtyDecimalPoint { get; set; }
        public string? QtyDecString { get; set; }

        // Application Features
        public bool DBSalSummary { get; set; }
        public bool MultiCurr { get; set; }
        public bool batch { get; set; }
        public bool SlNoTrack { get; set; }
        public bool TagAccMstr { get; set; }
        public bool additionalCost { get; set; }

        // Company and Financial-Year Connections
        public string CompShortName { get; set; } = string.Empty;
        public string PrevYearConn { get; set; } = string.Empty;
        public string NextYearConn { get; set; } = string.Empty;
        public string PrevYRDB { get; set; } = string.Empty;
        public string NextYRDB { get; set; } = string.Empty;

        public Dictionary<string, string> PrevYearConns { get; set; } = new();
        public Dictionary<string, string> NextYearConns { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
        public DTOs.PaginationRequest pgRequest = new DTOs.PaginationRequest();

        // Account Configuration
        public int CashCD { get; set; }
        public int DiscCD { get; set; }
        public int VATCode { get; set; }

        // VRSYS / Branch Configuration
        public bool VRSYS { get; set; }
        public bool MULTIVRSYS { get; set; }
        public bool BranchYN { get; set; }
        public bool BrPrList { get; set; }
        public string VoucherTypeCodes { get; set; } = string.Empty;

        public int VRSYS_DEFBRANCH { get; set; }
        public int VRSYS_OTHBRANCH { get; set; }
        public string VRSYS_ALLOWEDBRANCHES { get; set; } = string.Empty;

        // Stock Configuration
        public bool StkIssRec { get; set; }
        public bool STKTRNSFRYN { get; set; }
        public int STKTRNSFRACC { get; set; }
        public int STKTRNSFRRECACC { get; set; }
        public bool STKMVACC { get; set; }
        public int STKMVACCD { get; set; }

        // Approval Configuration
        public bool ApprovalSystem { get; set; }
        public bool InventoryApproval { get; set; }

        // Company / User
        public int CompCode { get; set; }
        public int UserId { get; set; }

        // Database Configuration
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Product / E-Invoice Configuration
        public bool EInvoice { get; set; }
        public bool EInvoice_sec { get; set; }
        public string APIURL { get; set; } = string.Empty;
        public string EinvoiceMode { get; set; } = "Direct";
        public int EINVTYPE { get; set; }
        public bool BM { get; set; }
    }
}
