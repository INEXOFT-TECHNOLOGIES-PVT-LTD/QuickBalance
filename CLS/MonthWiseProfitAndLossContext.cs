using QuickBalance.API.DB;
using System.Data;

namespace QuickBalance.API.CLS
{
    public class MonthWiseProfitAndLossContext
    {
        public string[] months { get; set; } = new string[12];

        public decimal GrossProfit { get; set; } = 0;

        public bool RecFlag { get; set; } = false;

        public DataTable dt { get; set; } = new DataTable();

        public DataTable dtP { get; set; } = new DataTable();

        public DataTable dtTCr { get; set; } = new DataTable();

        public DataTable dtTDr { get; set; } = new DataTable();

        public DataTable dtTGPL { get; set; } = new DataTable();

        public DataTable dtTTot { get; set; } = new DataTable();

        public DataTable dtGPLB { get; set; } = new DataTable();

        public DataTable dtPNPL { get; set; } = new DataTable();

        public DataTable dtPCr { get; set; } = new DataTable();

        public DataTable dtPDr { get; set; } = new DataTable();

        public DataTable dtPTot { get; set; } = new DataTable();

        // -----------------------------------------

        public Variable Variables { get; set; }

        public Function Functions { get; set; }

        // -----------------------------------------

        public ClsDate clsDate { get; set; } = new ClsDate();

        public ClsTaxMas clsTaxMas { get; set; } = new ClsTaxMas();

        public ClsPrMast clsPrMast { get; set; } = new ClsPrMast();

        public ClsPrGrpMas clsPrGrpMas { get; set; } = new ClsPrGrpMas();

        public ClsStkDt clsStkDt { get; set; } = new ClsStkDt();

        public ClsAccMas clsAccMas { get; set; } = new ClsAccMas();

        // -----------------------------------------

        public DayBookDB dayBookDB { get; set; }

        public PurmDB purmDB { get; set; }

        public TaxMasDBbkp taxMasDB { get; set; }

        public PrMastDB prMastDB { get; set; }

        public PrGrpMasDB prGrpMasDB { get; set; }

        public StkDtDB stkDtDB { get; set; }

        public AccMasDB accMasDB { get; set; }
    }
}
