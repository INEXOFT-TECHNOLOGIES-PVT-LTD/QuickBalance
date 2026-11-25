using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class CurrencyConversionDetailRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Alternate currency is required.")]
        //public int AlternateCurrencyId { get; set; }
        public int CurrencyId { get; set; }

        [Range(typeof(decimal), "0.000001", "999999999999",
            ErrorMessage = "Standard rate must be greater than zero.")]
        public decimal StandardRate { get; set; }

        [Range(typeof(decimal), "0.000001", "999999999999",
            ErrorMessage = "Sell rate must be greater than zero.")]
        public decimal SellRate { get; set; }

        [Range(typeof(decimal), "0.000001", "999999999999",
            ErrorMessage = "Buy rate must be greater than zero.")]
        public decimal BuyRate { get; set; }
    }

    public class CurrencyConversionRequest
    {
        public DateTime? ConversionDate { get; set; }

        [Required(ErrorMessage = "Currency conversion details are required.")]
        [MinLength(1, ErrorMessage = "At least one currency conversion detail is required.")]
        public List<CurrencyConversionDetailRequest> Details { get; set; } = new();
    }


    public class CurrencyConversionResponse
    {
        public int Id { get; set; }

        public int? BaseCurrencyId { get; set; }

        public int CurrencyId { get; set; }

        public string? Symbol { get; set; }

        public DateTime? ConversionDate { get; set; }

        public decimal StandardRate { get; set; }

        public decimal SellRate { get; set; }

        public decimal BuyRate { get; set; }

        //public int? UserId { get; set; }

        //public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>GET /currency-conversions/{id}: mirrors the request shape.</summary>
    public class CurrencyConversionDetailsResponse
    {
        public DateTime? ConversionDate { get; set; }

        public List<CurrencyConversionDetailResponse> Details { get; set; } = new();
    }

    public class CurrencyConversionDetailResponse
    {
        public int Id { get; set; }

        public int CurrencyId { get; set; }

        public string? Symbol { get; set; }

        public decimal StandardRate { get; set; }

        public decimal SellRate { get; set; }

        public decimal BuyRate { get; set; }
    }

    public class CurrencyConversionCurrencyResponse
    {
        public int Id { get; set; }

        public string? Symbol { get; set; }

        public bool IsBaseCurrency { get; set; }

        public string? ConversionMode { get; set; }
    }
}