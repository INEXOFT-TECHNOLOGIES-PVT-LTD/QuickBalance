using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using System.Data;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    [Route("api/v1/currenciesssss")]
    [Authorize]
    public class CurrencyMasterController : ControllerBase
    {
        private readonly CurrMasDB currMasDB;

        public CurrencyMasterController(CurrMasDB currMasDB)
        {
            this.currMasDB = currMasDB;
        }

        // =========================================================
        // GET ALL
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] PaginationRequest request)
        {
            request.Normalize();

            DataTable dt =
                await currMasDB.AllCurrencies(request.Search);

            int totalRecords = dt.Rows.Count;

            int totalPages =
                (int)Math.Ceiling(
                    totalRecords / (double)request.PageSize
                );

            var data = dt.AsEnumerable()
                .Skip(request.Offset)
                .Take(request.PageSize)
                .Select(row => new
                {
                    code = Convert.ToInt32(row["CODE"]),

                    symbol =
                        Convert.ToString(row["SYMBOL"]) ?? "",

                    currencyString =
                        Convert.ToString(row["CURRSTRING"]) ?? "",

                    subString =
                        Convert.ToString(row["SUBSTRING"]) ?? "",

                    conversionMode =
                        Convert.ToInt32(row["CONVMODE"]),

                    baseFlag =
                        Convert.ToBoolean(row["BASEFLG"]),

                    secondSymbol =
                        Convert.ToString(row["SECSYMBOL"]) ?? "",

                    secondCurrencyString =
                        Convert.ToString(row["SECCURSTRNG"]) ?? "",

                    secondSubString =
                        Convert.ToString(row["SECSUBSTRNG"]) ?? ""
                })
                .ToList();

            return Ok(new
            {
                status = true,
                message = "Currency list retrieved successfully.",
                data,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalRecords,
                    totalPages
                }
            });
        }

        // =========================================================
        // GET BY ID
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            ClsCurrMas cls = new ClsCurrMas
            {
                Code = id
            };

            DataTable dt = await currMasDB.SearchByCode(cls);

            if (dt.Rows.Count == 0)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Currency not found."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Currency retrieved successfully.",
                data = ConvertRow(dt.Rows[0])
            });
        }

        // =========================================================
        // POST
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CurrencyMasterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency data is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Master Name must not be blank."
                });
            }

            if (request.ConversionMode != 1 &&
                request.ConversionMode != 2)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid conversion mode. Allowed values are 1 or 2."
                });
            }

            ClsCurrMas cls = new ClsCurrMas
            {
                Symbol = request.Symbol.Trim()
            };

            DataTable existing =
                await currMasDB.SearchBySymbol(cls);

            if (existing.Rows.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Master Name Already Exist."
                });
            }

            cls.CurrString = request.CurrencyString?.Trim() ?? "";
            cls.SubString = request.SubString?.Trim() ?? "";
            cls.ConvMode = request.ConversionMode;

            // Legacy behavior:
            // New currency is always non-base.
            cls.BaseFlg = false;

            cls.SecSymbol = request.SecondSymbol?.Trim() ?? "";
            cls.SecCurrString =
                request.SecondCurrencyString?.Trim() ?? "";
            cls.SecSubString =
                request.SecondSubString?.Trim() ?? "";

            int code = await currMasDB.Insert(cls);

            if (code <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency could not be created."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Currency created successfully.",
                data = new
                {
                    code
                }
            });
        }

        // =========================================================
        // PUT
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CurrencyMasterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency data is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Master Name must not be blank."
                });
            }

            if (request.ConversionMode != 1 &&
                request.ConversionMode != 2)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid conversion mode. Allowed values are 1 or 2."
                });
            }

            ClsCurrMas cls = new ClsCurrMas
            {
                Code = id,
                Symbol = request.Symbol.Trim()
            };

            DataTable existing =
                await currMasDB.SearchByCode(cls);

            if (existing.Rows.Count == 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency not found."
                });
            }

            bool validSymbol =
                await currMasDB.ValidateSymbolForUpdate(cls);

            if (!validSymbol)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Master Name Already Exist."
                });
            }

            DataRow currentRow = existing.Rows[0];

            cls.CurrString =
                request.CurrencyString?.Trim() ?? "";

            cls.SubString =
                request.SubString?.Trim() ?? "";

            cls.ConvMode =
                request.ConversionMode;

            // Preserve existing base flag.
            cls.BaseFlg =
                Convert.ToBoolean(currentRow["BASEFLG"]);

            cls.SecSymbol =
                request.SecondSymbol?.Trim() ?? "";

            cls.SecCurrString =
                request.SecondCurrencyString?.Trim() ?? "";

            cls.SecSubString =
                request.SecondSubString?.Trim() ?? "";

            int result = await currMasDB.Update(cls);

            if (result <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency could not be updated."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Currency updated successfully."
            });
        }

        // =========================================================
        // BASE CURRENCIES
        // =========================================================
        [HttpGet("base")]
        public async Task<IActionResult> GetBaseCurrencies()
        {
            ClsCurrMas cls = new ClsCurrMas
            {
                BaseFlg = true
            };

            DataTable dt =
                await currMasDB.CurrencyByBaseFlag(cls);

            return Ok(new
            {
                status = true,
                message = "Base currency list retrieved successfully.",
                data = ConvertToList(dt)
            });
        }

        // =========================================================
        // ALTERNATE CURRENCIES
        // =========================================================
        [HttpGet("alternate")]
        public async Task<IActionResult> GetAlternateCurrencies()
        {
            ClsCurrMas cls = new ClsCurrMas
            {
                BaseFlg = false
            };

            DataTable dt =
                await currMasDB.CurrencyByBaseFlag(cls);

            return Ok(new
            {
                status = true,
                message = "Alternate currency list retrieved successfully.",
                data = ConvertToList(dt)
            });
        }

        // =========================================================
        // SIMPLE LIST
        // =========================================================
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            DataTable dt = await currMasDB.List();

            return Ok(new
            {
                status = true,
                message = "Currency list retrieved successfully.",
                data = ConvertToList(dt)
            });
        }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            ClsCurrMas cls = new ClsCurrMas
            {
                Code = id
            };

            DataTable existing =
                await currMasDB.SearchByCode(cls);

            if (existing.Rows.Count == 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency not found."
                });
            }

            bool isBase =
                Convert.ToBoolean(existing.Rows[0]["BASEFLG"]);

            if (isBase)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Base currency cannot be deleted."
                });
            }

            int result = await currMasDB.Delete(id);

            if (result <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Currency could not be deleted."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Currency deleted successfully."
            });
        }

        // =========================================================
        // CONVERT DATATABLE
        // =========================================================
        private static List<Dictionary<string, object?>> ConvertToList(
            DataTable dt)
        {
            var list =
                new List<Dictionary<string, object?>>();

            foreach (DataRow row in dt.Rows)
            {
                var item =
                    new Dictionary<string, object?>();

                foreach (DataColumn column in dt.Columns)
                {
                    object value = row[column];

                    item[column.ColumnName] =
                        value == DBNull.Value ? null : value;
                }

                list.Add(item);
            }

            return list;
        }

        private static object ConvertRow(DataRow row)
        {
            return new
            {
                code = Convert.ToInt32(row["CODE"]),
                symbol = Convert.ToString(row["SYMBOL"]) ?? "",
                currencyString =
                    Convert.ToString(row["CURRSTRING"]) ?? "",
                subString =
                    Convert.ToString(row["SUBSTRING"]) ?? "",
                conversionMode =
                    Convert.ToInt32(row["CONVMODE"]),
                baseFlag =
                    Convert.ToBoolean(row["BASEFLG"]),
                secondSymbol =
                    Convert.ToString(row["SECSYMBOL"]) ?? "",
                secondCurrencyString =
                    Convert.ToString(row["SECCURSTRNG"]) ?? "",
                secondSubString =
                    Convert.ToString(row["SECSUBSTRNG"]) ?? ""
            };
        }
    }
}