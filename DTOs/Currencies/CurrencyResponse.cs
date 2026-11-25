namespace QuickBalance.API.DTOs.Currencies
{

    public class CurrencyResponse
    {
        public int Id { get; set; }

        public string? Symbol { get; set; }

        public string? CurrString { get; set; }

        public string? SubString { get; set; }

        public int? ConvMode { get; set; }

        public bool BaseFlg { get; set; }

        public string? SecSymbol { get; set; }

        public string? SecCurrString { get; set; }

        public string? SecSubString { get; set; }
    }
}
