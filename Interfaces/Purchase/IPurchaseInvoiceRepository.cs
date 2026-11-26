using QuickBalance.API.DTOs.Purchase;

namespace QuickBalance.API.Interfaces.Purchase
{
    public interface IPurchaseInvoiceRepository
    {
        // ---- Lookups (read only) ----    
        Task<PurchaseSettings> GetSettingsAsync();
        Task<SupplierInfo?> GetSupplierAsync(int supplierCode);
        Task<IReadOnlyDictionary<int, ProductInfo>> GetProductsAsync(IEnumerable<int> productCodes, bool tagAccMstr);
        Task<CurrencyInfo?> GetCurrencyAsync(int currencyCode);
        Task<ExistingPurchaseHeader?> GetExistingHeaderAsync(int vrNo);
        Task<bool> SupplierBillNoExistsAsync(int supplierCode, string billNo, int excludeVrNo);

        // ---- Writes (each runs in ONE SQL transaction) ----    
        Task<PurchaseInvoiceSaveResult> CreateAsync(PurchaseInvoiceSaveModel model);
        Task<PurchaseInvoiceSaveResult> UpdateAsync(PurchaseInvoiceSaveModel model);

        // ---- Read ----    
        Task<PurchaseInvoiceResponse?> GetByIdAsync(int vrNo);
    }

    // =====================================================================    
    // Lookup models    
    // =====================================================================    

    /// <summary>Settings the Save depends on (review §12.3).</summary>    
    public sealed class PurchaseSettings
    {
        public int DecimalPoint { get; set; } = 2;        // CONFIGTB.DECPOINTS    
        public int QtyDecimalPoint { get; set; } = 2;     // CONFIGTB.QTYDECPOINTS    
        public bool InvAmtEditPi { get; set; }            // CONFIGTB.INVAMTEDITPI    
        public bool DateBlock { get; set; }               // CONFIGTB date block flag    
        public DateTime? BlockDate { get; set; }          // CONFIGTB.BLOCKDT    
        public DateTime? LastVrDate { get; set; }         // CONFIGTB.LASTVRDATE    
        public bool TagAccMstr { get; set; }              // CONFIGTB tag-account flag    
        public bool VatFlg { get; set; } = true;          // company master VATFLG (Home.cs:4796)    
        public int CashCode { get; set; }                 // FYDETAILS.CASHCD    
        public int VatCode { get; set; }                  // FYDETAILS.VATCD    
        public DateTime FyStartDate { get; set; }
        public DateTime FyEndDate { get; set; }
    }

    public sealed class SupplierInfo
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVatParty { get; set; }
        public bool IsForeign { get; set; }
        public bool IsGcc { get; set; }
    }

    /// <summary>PRMAST + TAXMAS + tax account (review §12.4).</summary>    
    public sealed class ProductInfo
    {
        public int ProductCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public int MainUnit { get; set; }                 // UNIT    
        public int AltUnit { get; set; }                  // ALTUNIT    
        public decimal ConvFact { get; set; }             // UNITCONV    
        public int ConvType { get; set; }                 // CONVTYPE    
        public int PurPriceType { get; set; }             // PURPRICETYPE    
        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }
        public string TaxType { get; set; } = string.Empty; // TTYPE ("E", "UR", ...)    
        public int TaxAccountCode { get; set; }           // PrMastDB.PurAcc    
    }

    public sealed class CurrencyInfo
    {
        public int Code { get; set; }
        public bool BaseFlag { get; set; }                // BASEFLG    
        public int ConvMode { get; set; }                 // CONVMODE: 1 = multiply, else divide    
    }

    public sealed class ExistingPurchaseHeader
    {
        public int VrNo { get; set; }
        public int OrdNo { get; set; }
        public string InvNoText { get; set; } = string.Empty;
        public int GrnNo { get; set; }
        public int YvrNo { get; set; }
    }

    // =====================================================================    
    // Save models (already calculated and converted to base currency)    
    // =====================================================================    

    public sealed class PurchaseInvoiceSaveModel
    {
        public bool IsNew { get; set; }
        public int VrNo { get; set; }                     // update only    
        public int OrdNo { get; set; }                    // update only (create: generated)    
        public int UserId { get; set; }
        public DateTime Now { get; set; }
        public PurchaseSettings Settings { get; set; } = new();
        public PurmRow Header { get; set; } = new();
        public List<PurtRow> Items { get; set; } = new();
        public List<PurdRow> Details { get; set; } = new();
        public List<StkdtRow> Stock { get; set; } = new();
        public List<ProductPriceUpdate> PriceUpdates { get; set; } = new();
    }

    public sealed class PurchaseInvoiceSaveResult
    {
        public int VrNo { get; set; }
        public int OrdNo { get; set; }
        public string InvNoText { get; set; } = string.Empty;
    }

    public sealed class PurmRow
    {
        public int VrNo { get; set; }
        public int OrdNo { get; set; }
        public string VrType { get; set; } = "P";
        public string InvNoText { get; set; } = string.Empty;
        public DateTime PurDate { get; set; }
        public int SupplierCode { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public int InFormat { get; set; }
        public string BType { get; set; } = string.Empty;
        public int AdjCd { get; set; }
        public int SlCode { get; set; }
        public int CurCode { get; set; }
        public decimal CurConRate { get; set; }
        public decimal NtValue { get; set; }
        public decimal TValue { get; set; }
        public decimal Tax { get; set; }
        public decimal Amount { get; set; }
        public decimal Freight { get; set; }
        public decimal DiscVal { get; set; }
        public decimal DiscP { get; set; }
        public decimal CusDuity { get; set; }
        public decimal CoinAdj { get; set; }
        public decimal Mrp { get; set; }
        public decimal CashPaid { get; set; }             // always 0 (part payment out of scope)    
        public int YvrNo { get; set; }                    // always 0    
        public int GrnNo { get; set; }                    // always 0 (no GRN tracking)    
        public string Remarks { get; set; } = string.Empty;
        public string ShipAddress { get; set; } = string.Empty;
        public decimal AdditionalCost { get; set; }
        public int AddCostAcCd { get; set; }
        public string Transferred { get; set; } = "N";
        public string AprvlSt { get; set; } = string.Empty;
    }

    public sealed class PurtRow
    {
        public int VrNo { get; set; }
        public int SlNo { get; set; }
        public int PrCode { get; set; }
        public string PrDesc { get; set; } = string.Empty;
        public int Unit { get; set; }
        public int AltUnit { get; set; }
        public decimal Qty { get; set; }
        public decimal ConvQty { get; set; }
        public decimal ConvFact { get; set; }
        public int ConvType { get; set; }
        public decimal PrRate { get; set; }
        public decimal ConvRate { get; set; }
        public decimal Mrp { get; set; }
        public decimal Disc { get; set; }
        public decimal DiscP { get; set; }
        public decimal LDiscPerc { get; set; }
        public decimal LDiscAmt { get; set; }
        public string LDiscType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public int TaxCode { get; set; }
        public decimal TaxAmt { get; set; }
        public int GdwCode { get; set; }
        public string Batch { get; set; } = string.Empty;
        public DateTime MfgDate { get; set; }
        public DateTime ExpDate { get; set; }
        public decimal AddCost { get; set; }
        public decimal ConvAddCost { get; set; }
        public string BType { get; set; } = string.Empty;
        public string StkFlg { get; set; } = "R";
        public int VatReasonCd { get; set; }
        public DateTime PurDate { get; set; }
    }

    public sealed class PurdRow
    {
        public int VrNo { get; set; }
        public string EType { get; set; } = string.Empty;   // T / A / D    
        public int Code { get; set; }
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal OrgTaxAmt { get; set; }
        public bool DFlag { get; set; }
        public int SlCode { get; set; }
        public int AdjCd { get; set; }
        public int GccCd { get; set; }
    }

    public sealed class StkdtRow
    {
        public int TrnNo { get; set; }                      // = ORDNO    
        public string Type { get; set; } = "R";
        public string VrType { get; set; } = "TR";
        public DateTime VrDate { get; set; }
        public int PrCode { get; set; }
        public int Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal ConvQty { get; set; }
        public int GdwCode { get; set; }
        public string Batch { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal ConvRate { get; set; }
        public decimal AddCost { get; set; }
        public decimal ConvAddCost { get; set; }
        public DateTime UTime { get; set; }
        public DateTime LuTime { get; set; }
    }

    /// <summary>UpdatePurRate: BASEPURPRICE for the main unit, else ALTPURPRICE (review §12.5).</summary>    
    public sealed class ProductPriceUpdate
    {
        public int ProductCode { get; set; }
        public bool IsMainUnit { get; set; }
        public decimal Price { get; set; }
    }
}
