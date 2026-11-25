namespace QuickBalance.API.DTOs
{
    public class BillwisePurchaseSummaryRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }


        // Remove Group Total checkbox
        public bool RemoveGroupTotal { get; set; }


        // Cash / Credit / Both
       // Cash
      // Credit
     // Both
        public string PaymentType { get; set; } = "Both";


        // true = Billwise
        // false = Supplierwise
        public bool BillWise { get; set; }


        // All Supplier / Selected Supplier
        public bool AllSupplier { get; set; }

        public int SupplierCode { get; set; }



        // All Godown / Selected Godown
        public bool AllGodown { get; set; }

        public int GodownCode { get; set; }



        // All Supplier Group / Selected Supplier Group
        public bool AllSupplierGroup { get; set; }

        public int SupplierGroupCode { get; set; }



        public bool IncludeUnapproved { get; set; }


        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}