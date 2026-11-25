namespace QuickBalance.API.DTOs;

public class PurchaseDetailReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public bool Purchase { get; set; }

    public int SupplierCode { get; set; }

    public bool RemoveGroupTotal { get; set; }

    public bool IncludeUnapproved { get; set; }

    public string VoucherTypeCodes { get; set; } = string.Empty;

 
}