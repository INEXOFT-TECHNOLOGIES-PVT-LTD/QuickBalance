public class DailyClosingRequest
{
    public DateTime AsOnDate { get; set; }

    public int AccountCode { get; set; }

    public bool IncludeUnapproved { get; set; }

    public string VoucherTypeCodes { get; set; } = string.Empty;
}