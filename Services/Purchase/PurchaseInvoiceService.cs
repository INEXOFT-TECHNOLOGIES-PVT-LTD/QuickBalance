//using Microsoft.EntityFrameworkCore.Infrastructure;
// VERIFY: namespace of ApiResponse<T>    
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs.Purchase;
using QuickBalance.API.Interfaces.Purchase;

namespace QuickBalance.API.Services.Purchase
{
    public class PurchaseInvoiceService : IPurchaseInvoiceService
    {
        private const string GenericSaveError = "Error in Data insertion";

        private readonly IPurchaseInvoiceRepository _repository;
        private readonly ILogger<PurchaseInvoiceService> _logger;

        public PurchaseInvoiceService(IPurchaseInvoiceRepository repository, ILogger<PurchaseInvoiceService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // =================================================================    
        // Public API    
        // =================================================================    

        public async Task<ApiResponse<PurchaseInvoiceCreateResponse>> CreateAsync(PurchaseInvoiceCreateRequest request, int userId)
        {
            try
            {
                var prepared = await PrepareAsync(request, existingVrNo: 0);
                var calc = Calculate(request, prepared);
                var model = BuildSaveModel(request, prepared, calc, userId, existing: null);

                var saved = await _repository.CreateAsync(model);
                return ApiResponse<PurchaseInvoiceCreateResponse>.Success(
                                    "Saved",
                                    ToCreateResponse(saved, calc));
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<PurchaseInvoiceCreateResponse>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Purchase Invoice.");
                return ApiResponse<PurchaseInvoiceCreateResponse>.Failure(GenericSaveError);
            }
        }

        public async Task<ApiResponse<PurchaseInvoiceCreateResponse>> UpdateAsync(int vrNo, PurchaseInvoiceUpdateRequest request, int userId)
        {
            try
            {
                if (vrNo <= 0)
                    throw new PurchaseInvoiceValidationException("Invalid purchase invoice id.");

                var existing = await _repository.GetExistingHeaderAsync(vrNo)
                    ?? throw new PurchaseInvoiceValidationException("Purchase invoice not found.");

                // Out-of-scope features: editing them here would leave GRN / payment rows inconsistent.    
                if (existing.GrnNo > 0)
                    throw new PurchaseInvoiceValidationException("This purchase has GRN tracking and cannot be edited through the API yet.");
                if (existing.YvrNo > 0)
                    throw new PurchaseInvoiceValidationException("This purchase has a part-payment voucher and cannot be edited through the API yet.");

                var prepared = await PrepareAsync(request, existingVrNo: vrNo);
                var calc = Calculate(request, prepared);
                var model = BuildSaveModel(request, prepared, calc, userId, existing);

                var saved = await _repository.UpdateAsync(model);
                return ApiResponse<PurchaseInvoiceCreateResponse>.Success(
                                     "Updated",
                                     ToCreateResponse(saved, calc));
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<PurchaseInvoiceCreateResponse>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Purchase Invoice {Id}.", vrNo);
                return ApiResponse<PurchaseInvoiceCreateResponse>.Failure(GenericSaveError);
            }
        }

        public async Task<ApiResponse<PurchaseInvoiceResponse>> GetByIdAsync(int vrNo)
        {
            try
            {
                if (vrNo <= 0)
                    throw new PurchaseInvoiceValidationException("Invalid purchase invoice id.");

                var invoice = await _repository.GetByIdAsync(vrNo);
                return invoice is null
                            ? ApiResponse<PurchaseInvoiceResponse>.Failure("Purchase invoice not found.")
                            : ApiResponse<PurchaseInvoiceResponse>.Success("Success", invoice);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<PurchaseInvoiceResponse>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Purchase Invoice {Id}.", vrNo);
                return ApiResponse<PurchaseInvoiceResponse>.Failure("Error getting purchase invoice.");
            }
        }

        // =================================================================    
        // Step 1: lookups + validation (review §4b, §12.6)    
        // =================================================================    

        private sealed class PreparedInvoice
        {
            public PurchaseSettings Settings { get; init; } = new();
            public SupplierInfo Supplier { get; init; } = new();
            public IReadOnlyDictionary<int, ProductInfo> Products { get; init; } = new Dictionary<int, ProductInfo>();
            public CurrencyConverter Converter { get; init; } = default!;
            public string BType { get; init; } = string.Empty;
            public int AdjCode { get; init; }
        }

        private async Task<PreparedInvoice> PrepareAsync(PurchaseInvoiceCreateRequest request, int existingVrNo)
        {
            if (request is null)
                throw new PurchaseInvoiceValidationException("Request body is required.");

            request.Items ??= new();
            request.AdditionalInfo ??= new();
            request.SupplierBillNo = (request.SupplierBillNo ?? string.Empty).Trim();

            var errors = new List<string>();
            var settings = await _repository.GetSettingsAsync();

            // ---- Unsupported features ----    
            if (request.TrackingEnabled)
                errors.Add("GRN tracking is not supported by this API version.");

            // ---- Header (legacy order: supplier, format, payment, items, total) ----    
            SupplierInfo? supplier = null;
            if (request.SupplierCode < 1)
                errors.Add("Invalid Supplier");
            else
            {
                supplier = await _repository.GetSupplierAsync(request.SupplierCode);
                if (supplier is null) errors.Add("Invalid Supplier");
            }

            if (request.InvoiceFormat < 1)
                errors.Add("Invalid Invoice Format Selected");

            if (settings.VatFlg && supplier is not null && request.InvoiceFormat >= 1)
                ValidateFormatAgainstSupplier(request.InvoiceFormat, supplier, errors);

            if (request.PaymentMode < 1 || request.PaymentMode > 4)
                errors.Add("Please Choose Method of Payment");

            if (request.Items.Count == 0)
                errors.Add("Item Details Must not be blank");

            // ---- Ledgers (rows 25-26) ----    
            int adjCode = 0;
            if (request.PaymentMode == 1)
            {
                adjCode = request.CashLedgerCode > 0 ? request.CashLedgerCode : settings.CashCode;
                if (adjCode <= 0) errors.Add("Invalid Cash Ledger Selection");
            }
            else if (request.PaymentMode == 4)
            {
                adjCode = request.BankLedgerCode;
                if (adjCode <= 0) errors.Add("Invalid Bank Ledger Selection");
            }

            // ---- Cash supplier must use cash (row 36) ----    
            if (settings.CashCode > 0 && request.SupplierCode == settings.CashCode && request.PaymentMode != 1)
                errors.Add("Must choose Payment Mode as 'Cash' For Cash Billing");

            // ---- Dates: financial year range (decision §12.6) and date block (row 28) ----    
            var purDate = request.PurchaseDate.Date;
            if (settings.FyStartDate != default && settings.FyEndDate != default &&
                (purDate < settings.FyStartDate.Date || purDate > settings.FyEndDate.Date))
                errors.Add("Purchase date must be within the current financial year.");

            if (settings.DateBlock)
            {
                if (settings.BlockDate.HasValue && purDate <= settings.BlockDate.Value.Date)
                    errors.Add("Backdated Entries Not Allowed On This User. Please Contact Your Super To User.");
                else if (!settings.BlockDate.HasValue && settings.LastVrDate.HasValue && purDate < settings.LastVrDate.Value.Date)
                    errors.Add("Backdated Entries Not Allowed On This User. Please Contact Your Super To User.");
            }

            // ---- Supplier bill number (rows 29-30); compares VRNO with VRNO (fixes §10 item 2) ----    
            if (string.IsNullOrWhiteSpace(request.SupplierBillNo))
            {
                errors.Add("Missing invoice number.");
            }
            else if (request.SupplierCode > 0)
            {
                _logger.LogInformation(
                    "Checking supplier bill duplicate: SupplierCode={SupplierCode}, BillNo={BillNo}, ExistingVrNo={ExistingVrNo}",
                    request.SupplierCode,
                    request.SupplierBillNo,
                    existingVrNo);

                var isDuplicate = await _repository.SupplierBillNoExistsAsync(
                    request.SupplierCode,
                    request.SupplierBillNo,
                    existingVrNo);

                _logger.LogInformation(
                    "SupplierBillNoExistsAsync executed. Result={IsDuplicate}",
                    isDuplicate);

                if (isDuplicate)
                {
                    errors.Add(existingVrNo == 0
                        ? "Purchase Bill with same Invoice.No already is generated for the selected Supplier"
                        : "Purchase Bill with same Invoice.No is already generated for the selected Supplier");
                }
            }
            else
            {
                _logger.LogWarning(
                    "Duplicate check skipped because SupplierCode is invalid: {SupplierCode}",
                    request.SupplierCode);
            }

            // ---- Currency ----    
            CurrencyInfo? currency = null;
            if (request.CurrencyCode > 0)
            {
                currency = await _repository.GetCurrencyAsync(request.CurrencyCode);
                if (currency is null) errors.Add("Invalid currency.");
                else if (!currency.BaseFlag && request.ConversionRate <= 0) errors.Add("Invalid currency conversion rate.");
            }

            // ---- Items (rows 3, 31-34) ----    
            var productCodes = request.Items.Where(i => i.ProductCode > 0).Select(i => i.ProductCode).Distinct().ToList();
            var products = productCodes.Count == 0
                ? new Dictionary<int, ProductInfo>()
                : await _repository.GetProductsAsync(productCodes, settings.TagAccMstr);

            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                int row = i + 1;

                if (item.ProductCode <= 0 || !products.TryGetValue(item.ProductCode, out var product))
                {
                    errors.Add($"Invalid Product choosen (Row {row})");
                    continue;
                }
                //if (item.Qty <= 0)
                //    errors.Add($"Quantity is zero at Row {row}");
                //if (item.Price < 0)
                //    errors.Add($"Price is negative at Row {row}");
                //if (item.UnitCode <= 0 ||
                //    (item.UnitCode != product.MainUnit && (product.AltUnit <= 0 || item.UnitCode != product.AltUnit)))
                //    errors.Add($"Invalid Unit choosen (Row {row})");
                //if (product.TaxAccountCode <= 0)
                //    // SavePurD fails when no ETYPE='D' row can be written (claim 25)    
                //    errors.Add($"Missing purchase account for product '{product.Description}' at Row {row}.");
                //if (item.LineDiscountPercentage is < 0 or > 100 || item.DiscountPercentage is < 0 or > 100)
                //    errors.Add($"Invalid discount at Row {row}");
            }

            // ---- Additional info ----    
            var dupRows = request.AdditionalInfo.GroupBy(a => a.RowIndex).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupRows.Count > 0)
                errors.Add($"Duplicate additional info row(s): {string.Join(", ", dupRows)}");

            if (errors.Count > 0)
                throw new PurchaseInvoiceValidationException(errors);

            return new PreparedInvoice
            {
                Settings = settings,
                Supplier = supplier!,
                Products = products,
                Converter = new CurrencyConverter(currency, request.ConversionRate, settings.DecimalPoint),
                BType = MapBType(request.PaymentMode),
                AdjCode = adjCode
            };
        }

        /// <summary>Format ↔ supplier rules (§4b rows 14-16, 23-24).</summary>    
        private static void ValidateFormatAgainstSupplier(int format, SupplierInfo supplier, List<string> errors)
        {
            if (supplier.IsVatParty)
            {
                if (supplier.IsForeign && supplier.IsGcc && format != 6 && format != 7)
                    errors.Add("Invalid invoice type selection, please must choose invoice type as 'GCC Purchase' or 'Exempt' for outside party (GCC Supplier).");
                else if (supplier.IsForeign && !supplier.IsGcc && format != 8 && format != 7)
                    errors.Add("Invalid invoice type selection, please must choose invoice type as 'Outside GCC' or 'Exempt' for outside party (Outside GCC Supplier).");
                else if (!supplier.IsForeign && (format == 6 || format == 8))
                    errors.Add("Invalid invoice type selection, please must not choose invoice type as 'GCC Purchase' or 'Outside GCC' for inside party.");

                if (format == 4)
                    errors.Add("You Have Selected VAT Enabled Party.Please Chose Valid Invoice Type.");
            }
            else
            {
                // VERIFY exact conditions at P.cs:3617-3627 (derived from the messages)    
                if (supplier.IsForeign && supplier.IsGcc && format != 4 && format != 6)
                    errors.Add("You Have Selected VAT Disabled Party.Please must choose invoice type as 'Not Registered' or 'GCC Purchase' for outside party (GCC Supplier).");
                else if (supplier.IsForeign && !supplier.IsGcc && format != 4 && format != 8)
                    errors.Add("You Have Selected VAT Disabled Party.Please must choose invoice type as 'Not Registered' or 'Outside GCC' for outside party (Outside GCC Supplier).");
                else if (!supplier.IsForeign && format != 4)
                    errors.Add("You Have Selected VAT Disabled Party.Please Chose Valid Invoice Type.");
            }
        }

        /// <summary>PaymentId → BTYPE (claim 8).</summary>    
        private static string MapBType(int paymentMode) => paymentMode switch
        {
            1 => "C",
            2 => "R",
            3 => "CD",
            4 => "B",
            _ => string.Empty
        };

        // =================================================================    
        // Step 2: calculation (review §12.1, claims 30-33)    
        // =================================================================    

        private sealed class LineCalc
        {
            public PurchaseInvoiceItemRequest Item { get; init; } = default!;
            public ProductInfo Product { get; init; } = default!;
            public int SlNo { get; init; }
            public bool IsMainUnit { get; init; }
            public decimal ConvFact { get; init; }
            public decimal Qty { get; init; }
            public decimal ConvQty { get; init; }
            public decimal Price { get; init; }
            public decimal LDiscPerc { get; init; }
            public decimal LDiscAmt { get; init; }
            public string LDiscType { get; init; } = string.Empty;
            public decimal NetPrice { get; init; }
            public decimal Discountable { get; init; }
            public decimal DiscPerc { get; init; }
            public decimal DiscPrice { get; init; }
            public decimal Amount { get; init; }
            public decimal TaxPerc { get; init; }
            public decimal TaxAmount { get; init; }
            public decimal Total { get; init; }
            public decimal ConvRate { get; init; }
            public decimal GrossForAddCost { get; init; }
            public int GodownCode { get; init; }
            public decimal AddCost { get; set; }
            public decimal ConvAddCost { get; set; }
        }

        private sealed class TaxRow
        {
            public int TaxCode { get; init; }
            public decimal TaxPerc { get; init; }
            public decimal Amount { get; init; }
            public decimal TaxAmount { get; init; }
        }

        private sealed class InvoiceCalc
        {
            public List<LineCalc> Lines { get; } = new();
            public List<TaxRow> TaxSummary { get; } = new();
            public decimal DiscountableTotal { get; set; }
            public decimal AmountTotal { get; set; }      // Σ Amount (taxable)    
            public decimal TaxTotal { get; set; }
            public decimal Total { get; set; }            // Σ Total → txt_total    
            public decimal MrpTotal { get; set; }
            public decimal Freight { get; set; }
            public decimal DiscPerc { get; set; }
            public decimal DiscVal { get; set; }          // informational (PURM.DISCVAL)    
            public decimal CustomsDuty { get; set; }
            public decimal CustomsDutyPerc { get; set; }
            public decimal Vat { get; set; }              // invoice-level VAT: not supported → 0    
            public decimal RoundOff { get; set; }
            public decimal OtherAdditions { get; set; }
            public decimal Gross { get; set; }
            public decimal Net { get; set; }
            public decimal Payable { get; set; }
        }

        private static InvoiceCalc Calculate(PurchaseInvoiceCreateRequest request, PreparedInvoice p)
        {
            var s = p.Settings;
            int dp = s.DecimalPoint;
            int qdp = s.QtyDecimalPoint;
            int format = request.InvoiceFormat;
            var calc = new InvoiceCalc();

            var addInfo = request.AdditionalInfo.ToDictionary(a => a.RowIndex);
            decimal headerDiscPerc = addInfo.TryGetValue(1, out var discRow) ? discRow.Percentage ?? 0m : 0m;

            int slNo = 0;
            foreach (var item in request.Items)
            {
                var product = p.Products[item.ProductCode];
                bool isMain = item.UnitCode == product.MainUnit;
                decimal cf = product.ConvFact <= 0 ? 1m : product.ConvFact;

                // Step 5: inputs    
                decimal taxPerc = (s.VatFlg && format != 4 && format != 5) ? R(product.TaxPercentage, dp) : 0m;
                if (format == 5 && !string.Equals(product.TaxType, "E", StringComparison.OrdinalIgnoreCase))
                    taxPerc = 0m;

                decimal qty = R(item.Qty, qdp);
                bool priceIsSame = R(item.Price, dp) == item.Price;
                decimal price = (s.InvAmtEditPi && !priceIsSame) ? R(item.Price, 6) : R(item.Price, dp);

                // Step 6: tax-inclusive format 3    
                if (format == 3 && taxPerc != 0)
                    price = price / (1 + taxPerc / 100m);

                // Steps 7-8: price per main/alt unit. With the auto price flag (step 2) the quantity is    
                // never rescaled, only the converted price.    
                decimal convPrice = ConvertPrice(price, product, isMain, cf);

                // Step 11: line discount    
                decimal lDiscPerc = 0m, lDiscAmt = 0m;
                string lDiscType = string.Empty;
                if (item.LineDiscountPercentage.HasValue && item.LineDiscountPercentage.Value != 0)
                {
                    lDiscPerc = item.LineDiscountPercentage.Value;
                    lDiscAmt = R(price * lDiscPerc / 100m, dp);
                    lDiscType = "Discount Based on %";
                }
                else if (item.LineDiscountValue.HasValue && item.LineDiscountValue.Value != 0)
                {
                    lDiscAmt = item.LineDiscountValue.Value;
                    lDiscPerc = price == 0 ? 0m : R(lDiscAmt * 100m / price, dp);
                    lDiscType = "Absolute Value";
                }

                // Step 12: discountable base    
                decimal netPrice = price - lDiscAmt;
                decimal discountable = netPrice * qty;
                if (!priceIsSame) discountable = R(discountable, dp);

                // Step 13: invoice-level discount applied per line    
                decimal discPerc = item.DiscountPercentage ?? headerDiscPerc;
                decimal discPrice = R(netPrice * discPerc / 100m, 6);

                // Steps 14-16: amount, tax, total (grid cells are formatted to DecimalPoint)    
                decimal amount = R((netPrice - discPrice) * qty, dp);
                decimal taxAmt = R(amount * taxPerc / 100m, dp);
                decimal total = amount + taxAmt;

                decimal convRate = price == 0 ? 0m : R(convPrice * (netPrice - discPrice) / price, 6);

                // VERIFY: CONVQTY rule (P.cs:2679-2705); assumed main-unit quantity    
                decimal convQty = isMain ? qty : (product.ConvType == 0 ? qty * cf : qty / cf);

                calc.Lines.Add(new LineCalc
                {
                    Item = item,
                    Product = product,
                    SlNo = item.SlNo > 0 ? item.SlNo : ++slNo,
                    IsMainUnit = isMain,
                    ConvFact = cf,
                    Qty = qty,
                    ConvQty = R(convQty, 4),
                    Price = price,
                    LDiscPerc = lDiscPerc,
                    LDiscAmt = lDiscAmt,
                    LDiscType = lDiscType,
                    NetPrice = netPrice,
                    Discountable = discountable,
                    DiscPerc = discPerc,
                    DiscPrice = discPrice,
                    Amount = amount,
                    TaxPerc = taxPerc,
                    TaxAmount = taxAmt,
                    Total = total,
                    ConvRate = convRate,
                    GrossForAddCost = R(qty * price, dp),
                    GodownCode = item.GodownCode > 0 ? item.GodownCode : 1 // GetGodownByBranch() without VRSYS/branch    
                });
            }

            // CalculateAdditionalCost (§12.1): allocated on gross Qty × Price    
            decimal grossBase = calc.Lines.Sum(l => l.GrossForAddCost);
            foreach (var l in calc.Lines)
            {
                if (request.AdditionalCost > 0 && grossBase > 0 && l.Qty > 0)
                {
                    l.AddCost = (l.GrossForAddCost / grossBase) * request.AdditionalCost / l.Qty;
                    l.ConvAddCost = l.IsMainUnit
                        ? l.AddCost
                        : (l.Product.ConvType == 1 ? l.AddCost * l.ConvFact : l.AddCost / l.ConvFact);
                }
            }

            // SubTotal (claim 30)    
            calc.DiscountableTotal = calc.Lines.Sum(l => l.Discountable);
            calc.AmountTotal = calc.Lines.Sum(l => l.Amount);
            calc.TaxTotal = calc.Lines.Sum(l => l.TaxAmount);
            calc.Total = calc.Lines.Sum(l => l.Total);
            calc.MrpTotal = calc.Lines.Sum(l => l.Item.Mrp * l.Qty);

            // TaxSummary (claim 33), chk_vat off    
            foreach (var g in calc.Lines.GroupBy(l => l.Product.TaxCode))
            {
                calc.TaxSummary.Add(new TaxRow
                {
                    TaxCode = g.Key,
                    TaxPerc = g.First().TaxPerc,
                    Amount = R(g.Sum(l => l.Amount), dp),
                    TaxAmount = format == 4 ? 0m : R(g.Sum(l => l.TaxAmount), dp)
                });
            }

            // AddInfoCalculation (claim 31)    
            calc.Freight = addInfo.TryGetValue(0, out var fr) ? fr.Value : 0m;
            calc.DiscPerc = headerDiscPerc;
            calc.DiscVal = calc.DiscountableTotal * headerDiscPerc / 100m;

            if (addInfo.TryGetValue(2, out var cdRow))
            {
                decimal cdBase = calc.Total + calc.Freight;
                if (cdRow.Percentage.HasValue)
                {
                    calc.CustomsDutyPerc = cdRow.Percentage.Value;
                    calc.CustomsDuty = cdBase * calc.CustomsDutyPerc / 100m;
                }
                else
                {
                    calc.CustomsDuty = cdRow.Value;
                    calc.CustomsDutyPerc = cdBase == 0 ? 0m : cdRow.Value * 100m / cdBase;
                }
            }

            calc.Vat = 0m; // row 3: invoice-level VAT (chk_vat) not supported    
            calc.RoundOff = addInfo.TryGetValue(4, out var ro) ? ro.Value : 0m;
            calc.OtherAdditions = request.AdditionalInfo.Where(a => a.RowIndex > 4).Sum(a => a.Value);

            // NetCalculation (claim 32): discount is already inside the line amounts    
            calc.Gross = R(calc.Total + calc.Freight, dp);
            calc.Net = R(calc.Gross + calc.CustomsDuty + calc.Vat, dp);
            calc.Payable = R(calc.Net + calc.RoundOff + calc.OtherAdditions, dp);

            // ---- Post-calculation validation (rows 19-20 + client cross-check) ----    
            var errors = new List<string>();
            if (calc.Payable < 0)
                errors.Add("Invoice net amount is less than zero.");

            if (request.ExpectedTaxDetails is { Count: > 0 })
                CompareTaxSummary(request.ExpectedTaxDetails, calc.TaxSummary, errors);

            if (errors.Count > 0)
                throw new PurchaseInvoiceValidationException(errors);

            return calc;
        }

        /// <summary>Converted price for the PURT/STKDT CONVRATE (steps 7-8).</summary>    
        private static decimal ConvertPrice(decimal price, ProductInfo product, bool isMain, decimal cf)
        {
            switch (product.PurPriceType)
            {
                case 0: // price on main unit    
                    if (isMain) return price;
                    return product.ConvType == 0 ? price / cf : price * cf;
                case 1: // price on alt unit    
                    if (isMain) return price;
                    return product.ConvType == 0 ? price / cf : price * cf;
                default: // type 2    
                    if (isMain) return price;
                    return product.ConvType == 0 ? price / cf : price * cf;
            }
        }

        private static void CompareTaxSummary(
            List<PurchaseInvoiceTaxDetailRequest> expected, List<TaxRow> actual, List<string> errors)
        {
            const decimal tolerance = 0.01m;
            foreach (var e in expected)
            {
                var a = actual.FirstOrDefault(x => x.TaxCode == e.TaxCode);
                if (a is null)
                {
                    errors.Add($"Tax summary mismatch: tax code {e.TaxCode} is not used by any item.");
                    continue;
                }
                if (Math.Abs(a.Amount - e.TaxableAmount) > tolerance || Math.Abs(a.TaxAmount - e.TaxAmount) > tolerance)
                    errors.Add($"Tax summary mismatch for tax code {e.TaxCode}: expected {e.TaxableAmount}/{e.TaxAmount}, calculated {a.Amount}/{a.TaxAmount}.");
            }
        }

        // =================================================================    
        // Step 3: map to table rows, base currency (review §2, §12.2)    
        // =================================================================    

        private static PurchaseInvoiceSaveModel BuildSaveModel(
            PurchaseInvoiceCreateRequest request,
            PreparedInvoice p,
            InvoiceCalc calc,
            int userId,
            ExistingPurchaseHeader? existing)
        {
            var c = p.Converter;
            var s = p.Settings;
            var now = DateTime.Now; // legacy uses local time throughout    
            var purDate = request.PurchaseDate.Date;

            var model = new PurchaseInvoiceSaveModel
            {
                IsNew = existing is null,
                VrNo = existing?.VrNo ?? 0,
                OrdNo = existing?.OrdNo ?? 0,
                UserId = userId,
                Now = now,
                Settings = s
            };

            // ---- PURM (claims 5-13) ----    
            model.Header = new PurmRow
            {
                VrNo = model.VrNo,
                OrdNo = model.OrdNo,
                VrType = "P",
                InvNoText = existing?.InvNoText ?? string.Empty, // create: generated in the repository    
                PurDate = purDate,
                SupplierCode = request.SupplierCode,
                BillNo = request.SupplierBillNo,
                BillDate = (request.SupplierBillDate ?? request.PurchaseDate).Date,
                InFormat = request.InvoiceFormat,
                BType = p.BType,
                AdjCd = p.AdjCode,
                SlCode = request.CostCentreCode,
                CurCode = request.CurrencyCode,
                CurConRate = request.CurrencyCode > 0 ? request.ConversionRate : 1m,
                NtValue = c.ToBase(calc.Total),
                TValue = c.ToBase(calc.AmountTotal),
                Tax = c.ToBase(calc.TaxTotal),
                Amount = c.ToBase(calc.Payable),
                Freight = c.ToBase(calc.Freight),
                DiscVal = c.ToBase(calc.DiscVal),
                DiscP = R(calc.DiscPerc, 4),
                CusDuity = c.ToBase(calc.CustomsDuty),
                CoinAdj = c.ToBase(calc.RoundOff),
                Mrp = c.ToBase(calc.MrpTotal),
                CashPaid = 0m,       // claim 12: no part payment    
                YvrNo = 0,
                GrnNo = 0,           // claim 11: TrackingEnabled = false    
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                ShipAddress = request.ShipAddress?.Trim() ?? string.Empty,
                AdditionalCost = c.ToBase(request.AdditionalCost),
                AddCostAcCd = request.AdditionalCostAccountCode,
                Transferred = "N",
                AprvlSt = string.Empty // approval system not supported    
            };

            // ---- PURT + STKDT + PRMAST price (claims 14-22, 26-28, §12.5) ----    
            foreach (var l in calc.Lines)
            {
                decimal prRate = s.InvAmtEditPi ? c.ToBaseActual(l.Price) : c.ToBase(l.Price);
                decimal addCost = (l.AddCost > 0 || l.ConvAddCost > 0) ? c.ToFourBase(l.AddCost) : 0m;
                decimal convAddCost = (l.AddCost > 0 || l.ConvAddCost > 0) ? c.ToFourBase(l.ConvAddCost) : 0m;

                model.Items.Add(new PurtRow
                {
                    SlNo = l.SlNo,
                    PrCode = l.Product.ProductCode,
                    PrDesc = l.Product.Description,
                    Unit = l.Item.UnitCode,
                    AltUnit = l.Product.AltUnit,
                    Qty = l.Qty,
                    ConvQty = l.ConvQty,
                    ConvFact = l.ConvFact,
                    ConvType = l.Product.ConvType,
                    PrRate = prRate,
                    ConvRate = c.ToBaseActual(l.ConvRate),
                    Mrp = c.ToBase(l.Item.Mrp),
                    Disc = c.ToBaseActual(l.DiscPrice),
                    DiscP = R(l.DiscPerc, 4),
                    LDiscPerc = R(l.LDiscPerc, 4),
                    LDiscAmt = c.ToBase(l.LDiscAmt),
                    LDiscType = l.LDiscType,
                    Amount = c.ToBase(l.Amount),
                    // Legacy quirk NOT reproduced: format 4 stored the invoice discount amount here (claim 17)    
                    Tax = l.TaxPerc,
                    TaxCode = l.Product.TaxCode,
                    TaxAmt = c.ToBase(l.TaxAmount),
                    GdwCode = l.GodownCode,
                    Batch = l.Item.Batch?.Trim() ?? string.Empty,
                    MfgDate = (l.Item.MfgDate ?? purDate).Date,
                    ExpDate = (l.Item.ExpiryDate ?? purDate).Date,
                    AddCost = addCost,
                    ConvAddCost = convAddCost,
                    BType = p.BType,
                    StkFlg = "R",
                    VatReasonCd = 0 // ShowReason is disabled in legacy (§12.4)    
                });

                model.Stock.Add(new StkdtRow
                {
                    Type = "R",
                    VrType = "TR",
                    VrDate = purDate,
                    PrCode = l.Product.ProductCode,
                    Unit = l.Item.UnitCode,
                    Qty = l.Qty,
                    ConvQty = l.ConvQty,
                    GdwCode = l.GodownCode,
                    Batch = l.Item.Batch?.Trim() ?? string.Empty,
                    // claim 28: price − line discount − general discount, then base conversion    
                    Rate = c.ToBaseActual(l.Price - l.LDiscAmt - l.DiscPrice),
                    ConvRate = c.ToBaseActual(l.ConvRate),
                    AddCost = addCost,
                    ConvAddCost = convAddCost,
                    UTime = now,   // repository keeps the old UTIME on update    
                    LuTime = now
                });

                // UpdatePurRate: price − (line discount + invoice discount), document currency as in legacy    
                model.PriceUpdates.Add(new ProductPriceUpdate
                {
                    ProductCode = l.Product.ProductCode,
                    IsMainUnit = l.IsMainUnit,
                    Price = R(l.Price - (l.LDiscAmt + l.DiscPrice), 6)
                });
            }

            // ---- PURD section A: tax summary rows, ETYPE 'T' (claim 23) ----    
            foreach (var t in calc.TaxSummary.Where(t => t.Amount != 0))
            {
                model.Details.Add(new PurdRow
                {
                    EType = "T",
                    Code = t.TaxCode,
                    Amount = c.ToBase(t.Amount),
                    Tax = R(t.TaxPerc, 4),
                    TaxAmt = c.ToBase(t.TaxAmount),
                    OrgTaxAmt = c.ToBase(t.TaxAmount),
                    DFlag = true,
                    SlCode = request.CostCentreCode,
                    AdjCd = p.AdjCode,
                    GccCd = 0
                });
            }

            // ---- PURD section B: additional info rows, ETYPE 'A' (claim 24) ----    
            var addRows = request.AdditionalInfo.OrderBy(a => a.RowIndex).ToList();
            int lastIndex = addRows.Count == 0 ? -1 : addRows[^1].RowIndex;
            foreach (var a in addRows)
            {
                if (a.RowIndex == 3) continue; // VAT row is never inserted    

                decimal value = a.RowIndex switch
                {
                    1 => calc.DiscVal,
                    2 => calc.CustomsDuty,
                    _ => a.Value
                };
                if (value == 0 && a.RowIndex != lastIndex) continue;

                decimal baseAmount = a.RowIndex == 2 ? calc.Gross : calc.Total;
                decimal perc = a.RowIndex switch
                {
                    1 => calc.DiscPerc,
                    2 => calc.CustomsDutyPerc,
                    _ => a.Percentage ?? 0m
                };

                model.Details.Add(new PurdRow
                {
                    EType = "A",
                    Code = a.AccountCode,
                    Amount = c.ToBase(baseAmount),
                    Tax = R(perc, 4),       // VERIFY: which PURD column holds the % / value (P.cs:1692-1761)    
                    TaxAmt = c.ToBase(value),
                    OrgTaxAmt = 0m,
                    DFlag = a.RowIndex <= 4,
                    SlCode = request.CostCentreCode,
                    AdjCd = p.AdjCode,
                    GccCd = a.GccHeadCode
                });
            }

            // ---- PURD section C: tax-account allocation, ETYPE 'D' (claim 25) ----    
            foreach (var g in calc.Lines.GroupBy(l => l.Product.TaxAccountCode))
            {
                model.Details.Add(new PurdRow
                {
                    EType = "D",
                    Code = g.Key,
                    Amount = c.ToBase(g.Sum(l => l.Amount)),
                    Tax = 0m,
                    TaxAmt = 0m,
                    OrgTaxAmt = 0m,
                    DFlag = true,
                    SlCode = request.CostCentreCode,
                    AdjCd = p.AdjCode,
                    GccCd = 0
                });
            }

            if (!model.Details.Any(d => d.EType == "D"))
                throw new PurchaseInvoiceValidationException(GenericSaveError);

            return model;
        }

        private static PurchaseInvoiceCreateResponse ToCreateResponse(PurchaseInvoiceSaveResult saved, InvoiceCalc calc) => new()
        {
            VrNo = saved.VrNo,
            OrdNo = saved.OrdNo,
            InvNoText = saved.InvNoText,
            TotalAmount = calc.Total,
            TaxAmount = calc.TaxTotal,
            NetAmount = calc.Payable,
            TaxSummary = calc.TaxSummary.Select(t => new PurchaseInvoiceTaxSummaryResponse
            {
                TaxCode = t.TaxCode,
                TaxPercentage = t.TaxPerc,
                TaxableAmount = t.Amount,
                TaxAmount = t.TaxAmount
            }).ToList()
        };

        private static decimal R(decimal value, int decimals) =>
            Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        // =================================================================    
        // Currency conversion (CurrConvDB, review §12.2)    
        // =================================================================    

        private sealed class CurrencyConverter
        {
            private readonly CurrencyInfo? _currency;
            private readonly decimal _rate;
            private readonly int _decimalPoint;

            public CurrencyConverter(CurrencyInfo? currency, decimal rate, int decimalPoint)
            {
                _currency = currency;
                _rate = rate;
                _decimalPoint = decimalPoint;
            }

            public decimal ToBase(decimal x) => R(Convert(x), _decimalPoint);
            public decimal ToBaseActual(decimal x) => R(Convert(x), 6);
            public decimal ToFourBase(decimal x) => R(Convert(x), 4);

            private decimal Convert(decimal x)
            {
                if (_currency is null || _currency.BaseFlag) return x;
                if (_currency.ConvMode == 1) return x * _rate;
                return _rate != 0 ? x / _rate : x;
            }
        }
    }
}
