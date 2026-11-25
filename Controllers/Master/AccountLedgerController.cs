
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Repository;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using static QuickBalance.API.DTOs.AccountLedgerRequest;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/accounts")]
    [Tags("03. Account Ledger")]
    [Authorize]
    public class AccountLedgerController : ControllerBase
    {
        private readonly AccMasDB _accMasDB;
        private readonly CurrencyRepository _currencyRepository;
        private readonly LedgerGroupDB _ledgerGroupDB;
        private readonly ILogger<AccountLedgerController> _logger;

        public AccountLedgerController(
            AccMasDB accMasDB,
            LedgerGroupDB ledgerGroupDB,
            CurrencyRepository currencyRepository,
            ILogger<AccountLedgerController> logger )
        {
            _accMasDB = accMasDB;
            _ledgerGroupDB = ledgerGroupDB;
            _logger = logger;
            _currencyRepository = currencyRepository;
        }

        // ============================================================
        // GET ALL ACCOUNTS - PAGINATED
        // GET: api/v1/accounts
        // ============================================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AccountLedgerDtos>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationRequests request,
            [FromQuery] bool? active = null)
        {
            try
            {
                if (request.Page < 1)
                {   
                    return BadRequest(new ApiResponseWithError
                    {
                        Status = false,
                        Message = "Page must be greater than or equal to 1."
                    });
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    return BadRequest(new ApiResponseWithError
                    {
                        Status = false,
                        Message = "Page size must be between 1 and 100."
                    });
                }
                request.Normalizes();

                var result = await _accMasDB.GetAllAccounts(
                    request.Page,
                    request.PageSize,
                    request.Search,
                    active
                    //request.SortBy,
                    //request.SortDirection
                );

                var response = ApiResponse<IReadOnlyList<AccountLedgerDtos>>.SuccessWithPagination(
                    "Accounts retrieved successfully.",
                    result.Data,
                    new PaginationInfo
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = result.TotalRecords,
                        TotalPages = (int)Math.Ceiling(
                            result.TotalRecords / (double)request.PageSize)
                    });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts.");
                return StatusCode(500, ApiResponseWithError.Failure(
                    "An error occurred while retrieving accounts."));
            }
        }

        // ============================================================
        // GET ACCOUNT BY ID
        // GET: api/v1/accounts/1
        // ============================================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<AccountLedgerDtos>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid account id."
                    });
                }

                var accounts = await _accMasDB.SearchByIDs(
                    new ClsAccMas { Id = id });

                if (accounts == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account not found."
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Account retrieved successfully.",
                    data =accounts //accounts[0]
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // CREATE ACCOUNT
        // POST: api/v1/accounts
        // ============================================================
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] AccountLedgerRequest request)
        {
            try
            {

                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account data is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AccountName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account name is required."
                    });
                }

                if (request.GroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Ledger group is required."
                    });
                }

                string openingBalanceType =
                    string.IsNullOrWhiteSpace(request.OpeningBalanceType)
                        ? "CR"
                        : request.OpeningBalanceType.Trim().ToUpperInvariant();

                if (openingBalanceType != "CR" &&
                    openingBalanceType != "DR")
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening balance type must be CR or DR."
                    });
                }

                if ((request.OpeningBalance ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening balance cannot be negative."
                    });
                }

                if ((request.Depreciation ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Depreciation cannot be negative."
                    });
                }

                if ((request.VatPercentage ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "VAT percentage cannot be negative."
                    });
                }

                if (request.CurrencyFlag)
                {
                    if ((request.CurrencyCode ?? 0) <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Currency code is required when currency is enabled."
                        });
                    }

                    if ((request.CurrencyConversionRate ?? 0) <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Currency conversion rate must be greater than zero."
                        });
                    }
                }

                string accountName = request.AccountName.Trim();

                // Check duplicate account name.
                var nameMatches = await _accMasDB.GetAllByName(
                    new ClsAccMasT
                    {
                        AccName = accountName
                    });

                if (nameMatches.Count > 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account name already exists."
                    });
                }

                // Generate account code when not supplied.
                //string accountCode = request.AccountCode?.Trim() ?? "";
                string accountCode = "";

                
                if (request.GroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Ledger group is required."
                    });
                }
                else {
                    // LedgerGroupDB still uses DataTable.
                    DataTable groupTable =
                        await _ledgerGroupDB.SearchByID(request.GroupId);

                    if (groupTable.Rows.Count == 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Ledger group not found."
                        });
                    }

                    string groupCode =
                        Convert.ToString(
                            groupTable.Rows[0]["GROUPCODE"]) ?? "";

                    if (string.IsNullOrWhiteSpace(groupCode))
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Ledger group code is missing."
                        });
                    }
                    //int maxCode = await _accMasDB.GetMaxCode(
                    //    groupCode.Length,
                    //    new ClsAccMasT
                    //    {
                    //        LedgerGroupId = request.GroupId
                    //    });

                    int maxCode = await _accMasDB.GetMaxCode(groupCode);

                    accountCode = $"{groupCode}-{(maxCode + 1):D2}";

                    //accountCode =
                        //groupCode + "-" + (maxCode + 1).ToString("D2");
                }

                // Check duplicate account code.
                var codeMatches = await _accMasDB.GetAllByCode(
                    new ClsAccMasT
                    {
                        AcCode = accountCode
                    });

                if (codeMatches.Count > 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account code already exists."
                    });
                }

                var currencyId = await _currencyRepository.GetCurrencyByIdAsync(request.CurrencyCode ?? 0);
                if(currencyId == null && request.CurrencyFlag)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Currency not found."
                    });
                }

                // Debit balances are stored as negative values.
                decimal ob = request.OpeningBalance ?? 0;
                decimal openingBalance = openingBalanceType == "DR"
                    ? -ob
                    : ob;

                var account = new ClsAccMasT
                {
                    Id = 0,
                    Alias = request.Alias?.Trim() ?? "",
                    AccName = accountName,
                    SecName = request.SecondName?.Trim() ?? "",
                    Address = request.Address?.Trim() ?? "",
                    Phone = request.Phone?.Trim() ?? "",
                    Active = request.Active,
                    LogCd = GetUserId(),
                    Uitime = DateTime.Now,
                    AcCode = accountCode,

                    PrintName = string.IsNullOrWhiteSpace(request.PrintName)
                        ? accountName
                        : request.PrintName.Trim(),

                    LedgerGroupId = request.GroupId,
                    Depreciation = request.Depreciation ?? 0,
                    VatFlag = request.VatFlag,
                    VatPerc = request.VatPercentage ?? 0,
                    TaxRetFlag = request.TaxReturnFlag,

                    BankAccName = request.BankAccountName?.Trim() ?? "",
                    BankAccno = request.BankAccountNumber?.Trim() ?? "",
                    Branch = request.Branch?.Trim() ?? "",
                    IbanNo = request.IbanNumber?.Trim() ?? "",
                    SwiftCode = request.SwiftCode?.Trim() ?? "",

                    OpenBal = openingBalance,
                    CurConvRate = request.CurrencyFlag
                        ? request.CurrencyConversionRate!.Value
                        : 1,

                    CurCode = request.CurrencyFlag
                        ? request.CurrencyCode!.Value
                        : 0,

                    CurFlg = request.CurrencyFlag,

                    APRVLST = "",
                    APRVLABY = 0,
                    APRVLBBY = 0,
                    APRVLRDT = DateTime.MinValue,
                    DelFlg = false
                };


                int accountId = await _accMasDB.Insert(account);

                if (accountId <= 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Unable to create account."
                    });
                }

                return Ok (new
                {
                    status = true,
                    message = "Account created successfully.",
                    data = new
                    {
                        accountId,
                        accountCode = account.AcCode
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // UPDATE ACCOUNT
        // PUT: api/v1/accounts/1
        // ============================================================
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id,[FromBody] AccountLedgerUpdateRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid account id."
                    });
                }

                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account data is required."
                    });
                }

                var existingAccounts = await _accMasDB.SearchByIDs(
                    new ClsAccMas { Id = id });

                if (existingAccounts==null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account not found."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AccountName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account name is required."
                    });
                }

                if (request.GroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Ledger group is required."
                    });
                }

                string openingBalanceType =
                    string.IsNullOrWhiteSpace(request.OpeningBalanceType)
                        ? "CR"
                        : request.OpeningBalanceType.Trim().ToUpperInvariant();

                if (openingBalanceType != "CR" &&
                    openingBalanceType != "DR")
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening balance type must be CR or DR."
                    });
                }

                if ((request.OpeningBalance ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Opening balance cannot be negative."
                    });
                }

                if ((request.Depreciation ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Depreciation cannot be negative."
                    });
                }

                if ((request.VatPercentage ?? 0) < 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "VAT percentage cannot be negative."
                    });
                }

                if (request.CurrencyFlag)
                {
                    if ((request.CurrencyCode ?? 0) <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Currency code is required when currency is enabled."
                        });
                    }

                    if ((request.CurrencyConversionRate ?? 0) <= 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Currency conversion rate must be greater than zero."
                        });
                    }
                }


                // Verify that the selected ledger group exists.
                DataTable groupTable =
                    await _ledgerGroupDB.SearchByID(request.GroupId);

                if (groupTable.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Ledger group not found."
                    });
                }

                // Retrieve GROUPCODE.
                string groupCode =
                    Convert.ToString(groupTable.Rows[0]["GROUPCODE"])?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(groupCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Ledger group code is missing."
                    });
                }

                var currencyId = await _currencyRepository.GetCurrencyByIdAsync(request.CurrencyCode ?? 0);
                if (currencyId == null && request.CurrencyFlag)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Currency not found."
                    });
                }

                string accountName = request.AccountName.Trim();

                // Check duplicate account name, excluding this account.
                var nameMatches = await _accMasDB.GetAllByName(
                    new ClsAccMasT
                    {
                        Id = id,
                        AccName = accountName
                    });

                if (nameMatches.Any(x => x.Id != id))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account name already exists."
                    });
                }

                if (request.RowVersion == null || request.RowVersion.Length != 8)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "A valid row version is required for updating the account."
                    });
                }

                // Preserve the existing account code if no new code is supplied.
                string existingCode =
                    Convert.ToString(existingAccounts?.AcCode) ?? "";


                var existingGroupId = existingAccounts?.LedgerGroupId;

                string accountCode;

                if (existingGroupId != request.GroupId)
                {
                    int maxCode = await _accMasDB.GetMaxCode(groupCode);

                    accountCode = $"{groupCode}-{(maxCode + 1):D2}";
                }
                else
                {
                    accountCode = existingCode;
                }

                //string accountCode = !string.IsNullOrWhiteSpace(groupCode) ? groupCode : existingCode;
                //string.IsNullOrWhiteSpace(request.AccountCode)
                //    ? existingCode
                //    : request.AccountCode.Trim();

                var codeMatches = await _accMasDB.GetAllByCode(
                    new ClsAccMasT
                    {
                        Id = id,
                        AcCode = accountCode
                    });

                if (codeMatches.Any(x => x.Id != id))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account code already exists."
                    });
                }

                decimal ob = request.OpeningBalance ?? 0;
                decimal openingBalance = openingBalanceType == "DR"
                    ? -ob
                    : ob;

                var account = new ClsAccMasT
                {
                    Id = id,
                    Alias = request.Alias?.Trim() ?? "",
                    AccName = accountName,
                    SecName = request.SecondName?.Trim() ?? "",
                    Address = request.Address?.Trim() ?? "",
                    Phone = request.Phone?.Trim() ?? "",
                    Active = request.Active,
                    LogCd = GetUserId(),
                    Uitime = DateTime.Now,
                    AcCode = accountCode,

                    PrintName = string.IsNullOrWhiteSpace(request.PrintName)
                        ? accountName
                        : request.PrintName.Trim(),

                    LedgerGroupId = request.GroupId,
                    Depreciation = request.Depreciation ?? 0,
                    VatFlag = request.VatFlag,
                    VatPerc = request.VatPercentage ?? 0,
                    TaxRetFlag = request.TaxReturnFlag,

                    BankAccName = request.BankAccountName?.Trim() ?? "",
                    BankAccno = request.BankAccountNumber?.Trim() ?? "",
                    Branch = request.Branch?.Trim() ?? "",
                    IbanNo = request.IbanNumber?.Trim() ?? "",
                    SwiftCode = request.SwiftCode?.Trim() ?? "",

                    OpenBal = openingBalance,
                    CurConvRate = request.CurrencyFlag
                        ? request.CurrencyConversionRate!.Value
                        : 1,

                    CurCode = request.CurrencyFlag
                        ? request.CurrencyCode!.Value
                        : 0,

                    CurFlg = request.CurrencyFlag,

                    APRVLST = "",
                    APRVLABY = 0,
                    APRVLBBY = 0,
                    APRVLRDT = DateTime.MinValue,
                    DelFlg = false,
                    
                };

                int result = await _accMasDB.Update(account,request.RowVersion);


                if (result == 0)
                {
                    return StatusCode(400,new
                    {
                        status = false,
                        message = "Account was modified by another user. Please refresh and try again."
                    });
                }

                //if (result <= 0)
                //{
                //    return StatusCode(500, new
                //    {
                //        status = false,
                //        message = "Unable to update account."
                //    });
                //}

                return Ok(new
                {
                    status = true,
                    message = "Account updated successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // GET OPENING BALANCE
        // GET: api/v1/accounts/1/opening
        // ============================================================
        [HttpGet("{id:int}/opening")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<AccountOpeningResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOpening(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure("Invalid account ID."));
                }

                var opening = await _accMasDB.Opening(id);

                if (opening==0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure("Account not found."));
                }

                var response = new AccountOpeningResponse
                {
                    AccountId = id,
                    OpeningBalance = opening
                };

                return Ok(
                    ApiResponseWithoutPagination<AccountOpeningResponse>.Success(
                        "Opening balance retrieved successfully.",
                        response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opening balance for AccountId {AccountId}.", id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseWithError.Failure(
                        "Failed to retrieve opening balance."));
            }
        }
        public class AccountOpeningResponse
        {
            public int AccountId { get; set; }
            public decimal OpeningBalance { get; set; }
        }
        // ============================================================
        // GET ACCOUNTS BY GROUP
        // GET: api/v1/accounts/group/1
        // ============================================================




        [HttpGet("group/{groupId:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<AccountLedgerDtos>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAccountsByGroup(
            int groupId)
        {
            try
            {
                if (groupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid ledger group id."
                    });
                }

                var data = await _accMasDB.AccountsByGroupsIds(
                    new ClsAccMas { LedgerGroupId = groupId });

                if (data == null || data.Count<=0)
                {
                    return BadRequest(
                        ApiResponseWithError.Failure("Account not found."));
                }
                return Ok(new
                {
                    status = true,
                    message = "Accounts retrieved successfully.",
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
        private int GetUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = User.FindFirstValue("userId");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = User.FindFirstValue("userid");
            }

            return int.TryParse(value, out int userId)
                ? userId
                : 0;
        }

        // ============================================================
        // DELETE ACCOUNT - SOFT DELETE
        // DELETE: api/v1/accounts/1
        // ============================================================
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid account ID."
                    });
                }

                var existingAccounts = await _accMasDB.SearchByIDs(
                    new ClsAccMas { Id = id });

                if (existingAccounts == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Account not found."
                    });
                }

                var account = new ClsAccMasT
                {
                    Id = id,
                    Active = false,
                    DelFlg = true,
                    LogCd = GetUserId(),
                    Uitime = DateTime.Now
                };

                int result = await _accMasDB.UpdateDelFlg(account);

                if (result <= 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Already delete account."
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Account deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
    }
}


// ============================================================
// GET ALL ACCOUNTS BY GROUP
// GET: api/v1/accounts/group/1/all
// ============================================================
//[HttpGet("group/{groupId:int}/all")]
//public async Task<IActionResult> GetAccountsByGroupAll(
//    int groupId,
//    [FromQuery] bool unapproved = false)
//{
//    try
//    {
//        if (groupId <= 0)
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Invalid ledger group id."
//            });
//        }

//        var data = await _accMasDB.AccountsByGroupAlls(
//            new ClsAccMas { MGroup = groupId },
//            unapproved);

//        return Ok(new
//        {
//            status = true,
//            message = "Accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET ACCOUNTS BY MULTIPLE GROUPS
// GET: api/v1/accounts/groups?groupIds=1,2,3
// ============================================================
//[HttpGet("groups")]
//public async Task<IActionResult> GetAccountsByGroups(
//    [FromQuery] string groupIds,
//    [FromQuery] bool unapproved = false)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(groupIds))
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Group ids are required."
//            });
//        }

//        var data = await _accMasDB.AccountsByMulGroupRpts(
//            groupIds,
//            unapproved);

//        return Ok(new
//        {
//            status = true,
//            message = "Accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (ArgumentException ex)
//    {
//        return BadRequest(new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }   
//}

// ============================================================
// GET ACTIVE ACCOUNTS FOR LEDGER
// GET: api/v1/accounts/active-for-ledger?multiAccountCode=1,2
// ============================================================
//[HttpGet("active-for-ledger")]
//public async Task<IActionResult> GetActiveAccountsForLedger(
//    [FromQuery] string multiAccountCode,
//    [FromQuery] bool unapproved = false)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(multiAccountCode))
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Account codes are required."
//            });
//        }

//        var data = await _accMasDB.ActiveAccountsForLedgers(
//            unapproved,
//            multiAccountCode);

//        return Ok(new
//        {
//            status = true,
//            message = "Active accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (ArgumentException ex)
//    {
//        return BadRequest(new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET ACTIVE ACCOUNTS FOR LEDGER - FAST
// ============================================================
//[HttpGet("active-for-ledger-fast")]
//public async Task<IActionResult> GetActiveAccountsForLedgerFast(
//    [FromQuery] int groupCode,
//    [FromQuery] string openingTable,
//    [FromQuery] string transactionTable,
//    [FromQuery] bool unapproved = false)
//{
//    try
//    {
//        if (groupCode <= 0)
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Invalid group code."
//            });
//        }

//        if (string.IsNullOrWhiteSpace(openingTable))
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Opening table is required."
//            });
//        }

//        if (string.IsNullOrWhiteSpace(transactionTable))
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Transaction table is required."
//            });
//        }

//        var data = await _accMasDB.ActiveAccountsForLedger_Fasts(
//            groupCode,
//            openingTable,
//            transactionTable,
//            unapproved);

//        return Ok(new
//        {
//            status = true,
//            message = "Active ledger accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (ArgumentException ex)
//    {
//        return BadRequest(new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET CASH / BANK LEDGERS
// GET: api/v1/accounts/cash-bank
// ============================================================
//[HttpGet("cash-bank")]
//public async Task<IActionResult> GetCashOrBankLedgers()
//{
//    try
//    {
//        var data = await _accMasDB.GetCashOrBankLedgerss();

//        return Ok(new
//        {
//            status = true,
//            message = "Cash and bank ledgers retrieved successfully.",
//            data
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET ACCOUNTS FOR SALES
// GET: api/v1/accounts/for-sales
// ============================================================
//[HttpGet("for-sales")]
//public async Task<IActionResult> GetAccountsForSales()
//{
//    try
//    {
//        var data = await _accMasDB.AccountsForSaless();

//        return Ok(new
//        {
//            status = true,
//            message = "Sales accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET ACCOUNTS FOR PURCHASE
// GET: api/v1/accounts/for-purchase
// ============================================================
//[HttpGet("for-purchase")]
//public async Task<IActionResult> GetAccountsForPurchase()
//{
//    try
//    {
//        var data = await _accMasDB.AccountsForPurchases();

//        return Ok(new
//        {
//            status = true,
//            message = "Purchase accounts retrieved successfully.",
//            data
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET MULTI-CURRENCY OPENING
// GET: api/v1/accounts/multi-currency-opening
// ============================================================
//[HttpGet("multi-currency-opening")]
//public async Task<IActionResult> GetMultiCurrencyOpening(
//    [FromQuery] int accountId = 0,
//    //[FromQuery] int groupId = 0,
//    [FromQuery] int currencyCode = 0,
//    [FromQuery] bool multiCurrencyMode = false)
//{
//    try
//    {
//        if (currencyCode <= 0)
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Currency code is required."
//            });
//        }

//        if (accountId < 0 )//|| groupId < 0)
//        {
//            return BadRequest(new
//            {
//                status = false,
//                message = "Account id and group id cannot be negative."
//            });
//        }

//        var data = await _accMasDB.GetOPN_MultiCurrs(
//            new ClsAccMas
//            {
//                Id = accountId,
//                //MGroup = groupId
//            },
//            currencyCode,
//            multiCurrencyMode);

//        return Ok(new
//        {
//            status = true,
//            message = "Multi-currency opening retrieved successfully.",
//            data
//        });
//    }
//    catch (Exception ex)
//    {
//        return StatusCode(500, new
//        {
//            status = false,
//            message = ex.Message
//        });
//    }
//}

// ============================================================
// GET USER ID FROM JWT
// ============================================================

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using QuickBalance.API.CLS;
//using QuickBalance.API.DB;
//using QuickBalance.API.DTOs;
//using QuickBalance.API.DTOs.Respons_DTO;
//using System.Data;
//using System.Security.Claims;

//namespace QuickBalance.API.Controllers.Master
//{
//    [ApiController]
//    [Route("api/v1/accounts")]
//    [Authorize]
//    public class AccountLedgerController : ControllerBase
//    {
//        private readonly AccMasDB _accMasDB;
//        private readonly LedgerGroupDB _ledgerGroupDB;

//        public AccountLedgerController(
//         AccMasDB accMasDB,
//         LedgerGroupDB ledgerGroupDB)
//        {
//            _accMasDB = accMasDB;
//            _ledgerGroupDB = ledgerGroupDB;
//        }

//        // ============================================================
//        // GET ALL ACCOUNTS - PAGINATED
//        // ============================================================
//        [HttpGet]

//        [HttpGet]
//        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
//        {
//            try
//            {
//                request.Normalize();

//                var result = await _accMasDB.GetAllAccounts(
//                    request.Page,
//                    request.PageSize,
//                    request.Search,
//                    request.SortBy,
//                    request.SortDirection
//                );

//                var response = new PaginationResponse<AccountLedgerDto>
//                {
//                    Data = result.Data,
//                    Page = request.Page,
//                    PageSize = request.PageSize,
//                    TotalRecords = result.TotalRecords,
//                    TotalPages = (int)Math.Ceiling(
//                        result.TotalRecords / (double)request.PageSize)
//                };

//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }


//        // Compare values for sorting.
//        private static int CompareValues(object? a, object? b)
//        {
//            if (a == null || a == DBNull.Value)
//                return b == null || b == DBNull.Value ? 0 : -1;

//            if (b == null || b == DBNull.Value)
//                return 1;

//            if (decimal.TryParse(Convert.ToString(a), out decimal da) &&
//                decimal.TryParse(Convert.ToString(b), out decimal db))
//            {
//                return da.CompareTo(db);
//            }

//            return string.Compare(
//                Convert.ToString(a),
//                Convert.ToString(b),
//                StringComparison.OrdinalIgnoreCase);
//        }
//        // ============================================================
//        // GET ACCOUNT BY ID
//        // ============================================================
//        [HttpGet("{id:int}")]
//        public async Task<IActionResult> GetById(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid account id."
//                    });
//                }

//                var clsAccMas = new ClsAccMas
//                {
//                    Id = id
//                };

//                DataTable dt = await _accMasDB.SearchByID(clsAccMas);

//                if (dt.Rows.Count == 0)
//                {
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Account not found."
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Account retrieved successfully.",
//                    data = ConvertRow(dt.Rows[0])
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // POST CREATE ACCOUNT
//        // ============================================================
//        [HttpPost]
//        public async Task<IActionResult> Create(
//            [FromBody] AccountLedgerRequest request)
//        {
//            try
//            {
//                if (request == null)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Account data is required."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(request.AccountName))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Account name is required."
//                    });
//                }

//                if (request.GroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Ledger group is required."
//                    });
//                }

//                string openingBalanceType =
//                    string.IsNullOrWhiteSpace(request.OpeningBalanceType)
//                        ? "CR"
//                        : request.OpeningBalanceType.Trim().ToUpper();

//                if (openingBalanceType != "CR" &&
//                    openingBalanceType != "DR")
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Opening balance type must be CR or DR."
//                    });
//                }

//                if (request.OpeningBalance < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Opening balance cannot be negative."
//                    });
//                }

//                if (request.Depreciation < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Depreciation cannot be negative."
//                    });
//                }

//                if (request.VatPercentage < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "VAT percentage cannot be negative."
//                    });
//                }

//                if (request.CurrencyFlag)
//                {
//                    if (request.CurrencyCode <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Currency code is required when currency is enabled."
//                        });
//                    }

//                    if (request.CurrencyConversionRate <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Currency conversion rate must be greater than zero."
//                        });
//                    }
//                }

//                // ----------------------------------------------------
//                // DUPLICATE ACCOUNT NAME
//                // ----------------------------------------------------
//                var nameCheck = new ClsAccMasT
//                {
//                    AccName = request.AccountName.Trim()
//                };

//                DataTable nameTable =
//                    await _accMasDB.GetAllByName(nameCheck);

//                if (nameTable.Rows.Count > 0)
//                {
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Account name already exists."
//                    });
//                }

//                // ----------------------------------------------------
//                // GENERATE ACCOUNT CODE IF NOT PROVIDED
//                // ----------------------------------------------------
//                string accountCode =
//                    request.AccountCode?.Trim() ?? "";

//                if (string.IsNullOrWhiteSpace(accountCode))
//                {
//                    // Fetch selected ledger group
//                    DataTable groupTable = await _ledgerGroupDB.SearchByID(request.GroupId);

//                    if (groupTable.Rows.Count == 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Ledger group not found."
//                        });
//                    }

//                    string groupCode =
//                        Convert.ToString(groupTable.Rows[0]["GROUPCODE"]) ?? "";

//                    if (string.IsNullOrWhiteSpace(groupCode))
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Ledger group code is missing."
//                        });
//                    }

//                    // Get maximum account-code suffix for this group
//                    var accountForCode = new ClsAccMasT
//                    {
//                        MGroup = request.GroupId
//                    };

//                    int maxCode = await _accMasDB.GetMaxCode(
//                        groupCode.Length,
//                        accountForCode
//                    );

//                    // Generate next account code
//                    accountCode =
//                        groupCode + "-" + (maxCode + 1).ToString("D2");
//                }
//                // ----------------------------------------------------
//                // DUPLICATE ACCOUNT CODE
//                // ----------------------------------------------------
//                var codeCheck = new ClsAccMasT
//                {
//                    AcCode = accountCode
//                };

//                DataTable codeTable =
//                    await _accMasDB.GetAllByCode(codeCheck);

//                if (codeTable.Rows.Count > 0)
//                {
//                    return Conflict(new
//                    {
//                        status = false,
//                        message = "Account code already exists."
//                    });
//                }

//                // ----------------------------------------------------
//                // OPENING BALANCE
//                // ----------------------------------------------------
//                decimal openingBalance = request.OpeningBalance;

//                if (openingBalanceType == "DR")
//                {
//                    openingBalance = -openingBalance;
//                }

//                // ----------------------------------------------------
//                // CREATE ACCOUNT OBJECT
//                // ----------------------------------------------------
//                int userId = GetUserId();

//                var clsAccMas = new ClsAccMasT
//                {
//                    Id = 0,
//                    Alias = request.Alias?.Trim() ?? "",
//                    AccName = request.AccountName.Trim(),
//                    SecName = request.SecondName?.Trim() ?? "",
//                    Address = request.Address?.Trim() ?? "",
//                    Phone = request.Phone?.Trim() ?? "",
//                    Active = request.IsActive,
//                    LogCd = userId,
//                    Uitime = DateTime.Now,
//                    AcCode = accountCode,

//                    PrintName =
//                        string.IsNullOrWhiteSpace(request.PrintName)
//                            ? request.AccountName.Trim()
//                            : request.PrintName.Trim(),

//                    MGroup = request.GroupId,
//                    Depreciation = request.Depreciation,
//                    VatFlag = request.VatFlag,
//                    VatPerc = request.VatPercentage,
//                    TaxRetFlag = request.TaxReturnFlag,

//                    BankAccName =
//                        request.BankAccountName?.Trim() ?? "",

//                    BankAccno =
//                        request.BankAccountNumber?.Trim() ?? "",

//                    Branch =
//                        request.Branch?.Trim() ?? "",

//                    IbanNo =
//                        request.IbanNumber?.Trim() ?? "",

//                    SwiftCode =
//                        request.SwiftCode?.Trim() ?? "",

//                    OpenBal = openingBalance,

//                    CurConvRate =
//                        request.CurrencyFlag
//                            ? request.CurrencyConversionRate
//                            : 1,

//                    CurCode =
//                        request.CurrencyFlag
//                            ? request.CurrencyCode
//                            : 0,

//                    CurFlg = request.CurrencyFlag,

//                    APRVLST = "",
//                    APRVLABY = 0,
//                    APRVLBBY = 0,
//                    APRVLRDT = DateTime.MinValue,
//                    DelFlg = false
//                };

//                // ----------------------------------------------------
//                // INSERT ACCOUNT
//                // ----------------------------------------------------
//                int accountId =
//                    await _accMasDB.Insert(clsAccMas);

//                if (accountId <= 0)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "Unable to create account."
//                    });
//                }

//                return StatusCode(201, new
//                {
//                    status = true,
//                    message = "Account created successfully.",
//                    data = new
//                    {
//                        accountId,
//                        accountName = clsAccMas.AccName,
//                        accountCode = clsAccMas.AcCode,
//                        groupId = clsAccMas.MGroup,
//                        openingBalance = clsAccMas.OpenBal,
//                        openingBalanceType
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // PUT UPDATE ACCOUNT
//        // ============================================================
//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> Update(
//            int id,
//            [FromBody] AccountLedgerRequest request)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid account id."
//                    });
//                }

//                if (request == null)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Account data is required."
//                    });
//                }

//                var existingAccount = new ClsAccMas
//                {
//                    Id = id
//                };

//                DataTable existingTable =
//                    await _accMasDB.SearchByID(existingAccount);

//                if (existingTable.Rows.Count == 0)
//                {
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Account not found."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(request.AccountName))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Account name is required."
//                    });
//                }

//                if (request.GroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Ledger group is required."
//                    });
//                }

//                string openingBalanceType =
//                    string.IsNullOrWhiteSpace(request.OpeningBalanceType)
//                        ? "CR"
//                        : request.OpeningBalanceType.Trim().ToUpper();

//                if (openingBalanceType != "CR" &&
//                    openingBalanceType != "DR")
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Opening balance type must be CR or DR."
//                    });
//                }

//                if (request.OpeningBalance < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Opening balance cannot be negative."
//                    });
//                }

//                if (request.Depreciation < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Depreciation cannot be negative."
//                    });
//                }

//                if (request.VatPercentage < 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "VAT percentage cannot be negative."
//                    });
//                }

//                if (request.CurrencyFlag)
//                {
//                    if (request.CurrencyCode <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Currency code is required when currency is enabled."
//                        });
//                    }

//                    if (request.CurrencyConversionRate <= 0)
//                    {
//                        return BadRequest(new
//                        {
//                            status = false,
//                            message = "Currency conversion rate must be greater than zero."
//                        });
//                    }
//                }

//                // ----------------------------------------------------
//                // DUPLICATE ACCOUNT NAME
//                // ----------------------------------------------------
//                var nameCheck = new ClsAccMasT
//                {
//                    Id = id,
//                    AccName = request.AccountName.Trim()
//                };

//                DataTable nameTable =
//                    await _accMasDB.GetAllByName(nameCheck);

//                foreach (DataRow row in nameTable.Rows)
//                {
//                    if (Convert.ToInt32(row["ID"]) != id)
//                    {
//                        return Conflict(new
//                        {
//                            status = false,
//                            message = "Account name already exists."
//                        });
//                    }
//                }

//                // ----------------------------------------------------
//                // DUPLICATE ACCOUNT CODE
//                // ----------------------------------------------------
//                if (!string.IsNullOrWhiteSpace(request.AccountCode))
//                {
//                    var codeCheck = new ClsAccMasT
//                    {
//                        Id = id,
//                        AcCode = request.AccountCode.Trim()
//                    };

//                    DataTable codeTable =
//                        await _accMasDB.GetAllByCode(codeCheck);

//                    foreach (DataRow row in codeTable.Rows)
//                    {
//                        if (Convert.ToInt32(row["ID"]) != id)
//                        {
//                            return Conflict(new
//                            {
//                                status = false,
//                                message = "Account code already exists."
//                            });
//                        }
//                    }
//                }

//                // ----------------------------------------------------
//                // OPENING BALANCE
//                // ----------------------------------------------------
//                decimal openingBalance = request.OpeningBalance;

//                if (openingBalanceType == "DR")
//                {
//                    openingBalance = -openingBalance;
//                }

//                int userId = GetUserId();

//                // ----------------------------------------------------
//                // BUILD MODEL
//                // ----------------------------------------------------
//                var clsAccMas = new ClsAccMasT
//                {
//                    Id = id,
//                    Alias = request.Alias?.Trim() ?? "",
//                    AccName = request.AccountName.Trim(),
//                    SecName = request.SecondName?.Trim() ?? "",
//                    Address = request.Address?.Trim() ?? "",
//                    Phone = request.Phone?.Trim() ?? "",
//                    Active = request.IsActive,
//                    LogCd = userId,
//                    Uitime = DateTime.Now,

//                    AcCode = request.AccountCode?.Trim() ?? "",

//                    PrintName =
//                        string.IsNullOrWhiteSpace(request.PrintName)
//                            ? request.AccountName.Trim()
//                            : request.PrintName.Trim(),

//                    MGroup = request.GroupId,
//                    Depreciation = request.Depreciation,
//                    VatFlag = request.VatFlag,
//                    VatPerc = request.VatPercentage,
//                    TaxRetFlag = request.TaxReturnFlag,

//                    BankAccName =
//                        request.BankAccountName?.Trim() ?? "",

//                    BankAccno =
//                        request.BankAccountNumber?.Trim() ?? "",

//                    Branch =
//                        request.Branch?.Trim() ?? "",

//                    IbanNo =
//                        request.IbanNumber?.Trim() ?? "",

//                    SwiftCode =
//                        request.SwiftCode?.Trim() ?? "",

//                    OpenBal = openingBalance,

//                    CurConvRate =
//                        request.CurrencyFlag
//                            ? request.CurrencyConversionRate
//                            : 1,

//                    CurCode =
//                        request.CurrencyFlag
//                            ? request.CurrencyCode
//                            : 0,

//                    CurFlg = request.CurrencyFlag,

//                    APRVLST = "",
//                    APRVLABY = 0,
//                    APRVLBBY = 0,
//                    APRVLRDT = DateTime.MinValue,
//                    DelFlg = false
//                };

//                // ----------------------------------------------------
//                // UPDATE
//                // ----------------------------------------------------
//                int result =
//                    await _accMasDB.Update(clsAccMas);

//                if (result <= 0)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "Unable to update account."
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Account updated successfully.",
//                    data = new
//                    {
//                        accountId = id,
//                        accountName = clsAccMas.AccName,
//                        accountCode = clsAccMas.AcCode,
//                        groupId = clsAccMas.MGroup,
//                        openingBalance = clsAccMas.OpenBal,
//                        openingBalanceType
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET OPENING BALANCE
//        // ============================================================
//        [HttpGet("{id:int}/opening")]
//        public async Task<IActionResult> GetOpening(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid account id."
//                    });
//                }

//                decimal opening = await _accMasDB.Opening(id);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Opening balance retrieved successfully.",
//                    data = new
//                    {
//                        accountId = id,
//                        openingBalance = opening
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACCOUNTS BY GROUP
//        // ============================================================
//        [HttpGet("group/{groupId:int}")]
//        public async Task<IActionResult> GetAccountsByGroup(
//            int groupId,
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                if (groupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid ledger group id."
//                    });
//                }

//                var clsAccMas = new ClsAccMas
//                {
//                    MGroup = groupId
//                };

//                DataTable dt =
//                    await _accMasDB.AccountsByGroupRpt(
//                        clsAccMas,
//                        unapproved);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ALL ACCOUNTS BY GROUP
//        // ============================================================
//        [HttpGet("group/{groupId:int}/all")]
//        public async Task<IActionResult> GetAccountsByGroupAll(
//            int groupId,
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                if (groupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid ledger group id."
//                    });
//                }

//                var clsAccMas = new ClsAccMas
//                {
//                    MGroup = groupId
//                };

//                DataTable dt =
//                    await _accMasDB.AccountsByGroupAll(
//                        clsAccMas,
//                        unapproved);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACCOUNTS BY MULTIPLE GROUPS
//        // ============================================================
//        [HttpGet("groups")]
//        public async Task<IActionResult> GetAccountsByGroups(
//            [FromQuery] string groupIds,
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(groupIds))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Group ids are required."
//                    });
//                }

//                DataTable dt =
//                    await _accMasDB.AccountsByMulGroupRpt(
//                        groupIds,
//                        unapproved);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACTIVE ACCOUNTS FOR LEDGER
//        // ============================================================
//        [HttpGet("active-for-ledger")]
//        public async Task<IActionResult> GetActiveAccountsForLedger(
//            [FromQuery] string multiAccountCode,
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(multiAccountCode))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Account codes are required."
//                    });
//                }

//                DataTable dt =
//                    await _accMasDB.ActiveAccountsForLedger(
//                        unapproved,
//                        multiAccountCode);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Active accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACTIVE ACCOUNTS FOR LEDGER - FAST
//        // ============================================================
//        [HttpGet("active-for-ledger-fast")]
//        public async Task<IActionResult> GetActiveAccountsForLedgerFast(
//            [FromQuery] int groupCode,
//            [FromQuery] string openingTable,
//            [FromQuery] string transactionTable,
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(openingTable))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Opening table is required."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(transactionTable))
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Transaction table is required."
//                    });
//                }

//                DataTable dt =
//                    await _accMasDB.ActiveAccountsForLedger_Fast(
//                        groupCode,
//                        openingTable,
//                        transactionTable,
//                        unapproved);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Active ledger accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET CASH / BANK LEDGERS
//        // ============================================================
//        [HttpGet("cash-bank")]
//        public async Task<IActionResult> GetCashOrBankLedgers(
//            [FromQuery] bool unapproved = false)
//        {
//            try
//            {
//                DataTable dt =
//                    await _accMasDB.GetCashOrBankLedgers(unapproved);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Cash and bank ledgers retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACCOUNTS FOR SALES
//        // ============================================================
//        [HttpGet("for-sales")]
//        public async Task<IActionResult> GetAccountsForSales()
//        {
//            try
//            {
//                DataTable dt = await _accMasDB.AccountsForSales();

//                return Ok(new
//                {
//                    status = true,
//                    message = "Sales accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET ACCOUNTS FOR PURCHASE
//        // ============================================================
//        [HttpGet("for-purchase")]
//        public async Task<IActionResult> GetAccountsForPurchase()
//        {
//            try
//            {
//                DataTable dt = await _accMasDB.AccountsForPurchase();

//                return Ok(new
//                {
//                    status = true,
//                    message = "Purchase accounts retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // GET MULTI-CURRENCY OPENING
//        // ============================================================
//        [HttpGet("multi-currency-opening")]
//        public async Task<IActionResult> GetMultiCurrencyOpening(
//            [FromQuery] int accountId = 0,
//            [FromQuery] int groupId = 0,
//            [FromQuery] int currencyCode = 0,
//            [FromQuery] bool multiCurrencyMode = false)
//        {
//            try
//            {
//                if (currencyCode <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Currency code is required."
//                    });
//                }

//                var clsAccMas = new ClsAccMas
//                {
//                    Id = accountId,
//                    MGroup = groupId
//                };

//                DataTable dt =
//                    await _accMasDB.GetOPN_MultiCurr(
//                        clsAccMas,
//                        currencyCode,
//                        multiCurrencyMode);

//                return Ok(new
//                {
//                    status = true,
//                    message = "Multi-currency opening retrieved successfully.",
//                    data = ConvertToList(dt)
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }

//        // ============================================================
//        // DATATABLE -> LIST
//        // ============================================================
//        private static List<Dictionary<string, object?>> ConvertToList(
//            DataTable table)
//        {
//            var list = new List<Dictionary<string, object?>>();

//            foreach (DataRow row in table.Rows)
//            {
//                list.Add(ConvertRow(row));
//            }

//            return list;
//        }

//        // ============================================================
//        // DATAROW -> DICTIONARY
//        // ============================================================
//        private static Dictionary<string, object?> ConvertRow(
//            DataRow row)
//        {
//            var result =
//                new Dictionary<string, object?>(
//                    StringComparer.OrdinalIgnoreCase);

//            foreach (DataColumn column in row.Table.Columns)
//            {
//                object value = row[column];

//                result[column.ColumnName] =
//                    value == DBNull.Value ? null : value;
//            }

//            return result;
//        }

//        // ============================================================
//        // GET USER ID FROM JWT
//        // ============================================================
//        private int GetUserId()
//        {
//            string? value =
//                User.FindFirstValue(ClaimTypes.NameIdentifier);

//            if (string.IsNullOrWhiteSpace(value))
//            {
//                value = User.FindFirstValue("userId");
//            }

//            if (string.IsNullOrWhiteSpace(value))
//            {
//                value = User.FindFirstValue("userid");
//            }

//            if (int.TryParse(value, out int userId))
//            {
//                return userId;
//            }

//            return 0;
//        }

//        // ============================================================
//        // DELETE ACCOUNT (SOFT DELETE)
//        // ============================================================
//        [HttpDelete("{id:int}")]
//        public async Task<IActionResult> Delete(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Invalid account ID."
//                    });
//                }

//                var account = new ClsAccMas
//                {
//                    Id = id
//                };

//                DataTable existingTable =
//                    await _accMasDB.SearchByID(account);

//                if (existingTable.Rows.Count == 0)
//                {
//                    return NotFound(new
//                    {
//                        status = false,
//                        message = "Account not found."
//                    });
//                }

//                var clsAccMas = new ClsAccMasT
//                {
//                    Id = id,
//                    Active = false,
//                    DelFlg = true,
//                    LogCd = GetUserId(),
//                    Uitime = DateTime.Now
//                };

//                int result =
//                    await _accMasDB.UpdateDelFlg(clsAccMas);

//                if (result <= 0)
//                {
//                    return StatusCode(500, new
//                    {
//                        status = false,
//                        message = "Unable to delete account."
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Account deleted successfully."
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    status = false,
//                    message = ex.Message
//                });
//            }
//        }
//    }
//}