namespace QuickBalance.API.CLS
{
    public class ClsDate
    {
        private string? dateFrom;
        public string DateFrom
        {
            get => dateFrom?.Replace("'", "''") ?? string.Empty;
            set => dateFrom = value;
        }

        private string? toDate;
        public string ToDate
        {
            get => toDate?.Replace("'", "''") ?? string.Empty;
            set => toDate = value;
        }
    }
}
