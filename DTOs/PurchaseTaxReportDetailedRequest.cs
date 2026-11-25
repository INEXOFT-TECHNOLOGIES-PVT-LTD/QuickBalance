namespace QuickBalance.API.DTOs;

public class PurchaseTaxReportDetailedRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int OrdNo { get; set; } = 0;
    public bool IncludeUnapproved { get; set; }

   
    public string VoucherTypeCodes { get; set; } = "";

  
}