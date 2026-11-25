using QuickBalance.API.DB;
using System.Data;

namespace QuickBalance.API.CLS
{
    public class ProfitAndLossContext
    {
        public decimal GrossProfit { get; set; } = 0;

        public decimal SIDN_Amount { get; set; } = 0;

        public decimal PL_Amount { get; set; } = 0;

        public decimal PIRN_Amount { get; set; } = 0;

        public bool RecFlag { get; set; } = false;

        public DataTable dt { get; set; } = new DataTable();

        public DataTable dtP { get; set; } = new DataTable();

        public DataTable dtP2 { get; set; } = new DataTable();

        public DataTable dtDate { get; set; } = new DataTable();

        public DataTable dtBrTrVr { get; set; } = new DataTable();

        // -----------------------------------------

        public Variable Variables { get; set; }

        public Function Functions { get; set; }

        // -----------------------------------------

        public ClsDate clsDate { get; set; } = new ClsDate();

        public ClsTaxMas clsTaxMas { get; set; } = new ClsTaxMas();

        public ClsPrMast clsPrMast { get; set; } = new ClsPrMast();

        public ClsPrGrpMas clsPrGrpMas { get; set; } = new ClsPrGrpMas();

        public ClsStkDt clsStkDt { get; set; } = new ClsStkDt();

        public ClsOpnDir clsOpnDir { get; set; } = new ClsOpnDir();

        public ClsAccMas clsAccMas { get; set; } = new ClsAccMas();

        // -----------------------------------------

        public DayBookDB dayBookDB { get; set; }

        public PurmDB purmDB { get; set; }

        public TaxMasDBbkp taxMasDB { get; set; }

        public PrMastDB prMastDB { get; set; }

        public PrGrpMasDB prGrpMasDB { get; set; }

        public StkDtDB stkDtDB { get; set; }

        public PlDB plDB { get; set; }

        public OpnDirDB opnDirDB { get; set; }

        public AccMasDB accMasDB { get; set; }

        public GrpLdgDB grpLdgDB { get; set; }
    }
}
