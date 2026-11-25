namespace QuickBalance.API.DTOs
{
    public class CurrencyMasterRequest
    {
        public string Symbol { get; set; } = "";
        public string CurrencyString { get; set; } = "";
        public string SubString { get; set; } = "";

        public int ConversionMode { get; set; }

        public string SecondSymbol { get; set; } = "";
        public string SecondCurrencyString { get; set; } = "";
        public string SecondSubString { get; set; } = "";
    }
}