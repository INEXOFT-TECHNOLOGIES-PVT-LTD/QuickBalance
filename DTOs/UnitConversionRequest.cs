namespace QuickBalance.API.DTOs
{
    public class UnitConversionRequest
    {
        public int MainUnit { get; set; }

        public int SubUnit { get; set; }

        public decimal ConversionFactor { get; set; }
    }
}