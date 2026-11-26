using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>
    /// One serial number coming back on a return line.
    /// The procedure checks that it belongs to the original sales invoice.
    /// Product and line number are taken from the parent line, so only the serial is needed.
    /// </summary>
    public class SalesReturnSaveSerialRequest
    {
        [Required(ErrorMessage = "SerialNo is required.")]
        [MaxLength(100)]
        public string SerialNo { get; set; } = string.Empty;
    }
}
