namespace QuickBalance.API.CLS
{
    public class ClsTrialBal
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool Alphabetic { get; set; }

        public bool WithoutBal { get; set; }

        public bool Detailed { get; set; }
    }
}
