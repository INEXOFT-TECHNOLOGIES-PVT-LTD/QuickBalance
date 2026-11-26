using QuickBalance.API.DTOs.Purchase;
// VERIFY: namespace of ApiResponse<T>    
using QuickBalance.API.CLS;

namespace QuickBalance.API.Interfaces.Purchase
{
    public interface IPurchaseInvoiceService
    {
        Task<ApiResponse<PurchaseInvoiceCreateResponse>> CreateAsync(PurchaseInvoiceCreateRequest request, int userId);
        Task<ApiResponse<PurchaseInvoiceCreateResponse>> UpdateAsync(int vrNo, PurchaseInvoiceUpdateRequest request, int userId);
        Task<ApiResponse<PurchaseInvoiceResponse>> GetByIdAsync(int vrNo);
    }

    /// <summary>    
    /// Business-rule failure. Derives from ArgumentException so it follows the    
    /// project rule "services throw ArgumentException for user-facing validation".    
    /// </summary>    
    public sealed class PurchaseInvoiceValidationException : ArgumentException
    {
        public IReadOnlyList<string> Errors { get; }

        public PurchaseInvoiceValidationException(IReadOnlyList<string> errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            Errors = errors;
        }

        public PurchaseInvoiceValidationException(string error)
            : this(new[] { error })
        {
        }
    }
}
