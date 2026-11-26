
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DB;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Respons_DTO;
using QuickBalance.API.Services;
using System.Security.Claims;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Route("api/v1/ledger-groups")]
    [Tags("02. LedgerGroup")]

    [Authorize]
    public class LedgerGroupController : ControllerBase
    {
        private readonly LedgerGroupDB ledgerGroupDB;
        private readonly ICurrentUser _currentUser;

        public LedgerGroupController(LedgerGroupDB ledgerGroupDB, ICurrentUser currentUser)
        {
            this.ledgerGroupDB = ledgerGroupDB;
            _currentUser = currentUser;
        }

        // ============================================================
        // GET ALL - PAGINATED
        // ============================================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<LedgerGroupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationRequests request,
            [FromQuery] bool? active = null)
        {
            //request.Normalizes();
            if (request.Page < 1)
            {
                return BadRequest(
                    ApiResponseWithError.Failure("Page must be greater than or equal to 1."));
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return BadRequest(
                    ApiResponseWithError.Failure("Page size must be between 1 and 100."));
            }
            var result = await ledgerGroupDB.GetAll(
                request.Search,
                request.Page,
                request.PageSize,
                active);

            int totalPages = (int)Math.Ceiling(
                result.TotalRecords / (double)request.PageSize);

            return Ok(new
            {
                status = true,
                message = "Ledger group list retrieved successfully.",
                data = result.Data,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalRecords = result.TotalRecords,
                    totalPages
                }
            });
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LedgerGroupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ledger group ID."
                });
            }

            var data = await ledgerGroupDB.SearchByIDs(id);

            if (data == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group not found."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Ledger group retrieved successfully.",
                data
            });
        }

        // ============================================================
        // CREATE
        // ============================================================
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] LedgerGroupRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group data is required."
                });
            }

            // GROUP NAME
            if (string.IsNullOrWhiteSpace(request.GroupName))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Name must not be blank."
                });
            }

            string groupName = request.GroupName.Trim();

            // FINAL ACCOUNT / CR-DR: normalised here, validated after the
            // main/sub-group branch (a sub-group inherits both from its parent).
            string finalAccount =(request.FinalAccount ?? "").Trim().ToUpperInvariant();

            string[] allowedFinalAccounts = { "P", "T", "B", "M" };

            string crDr = (request.CrDr ?? "").Trim().ToUpperInvariant();

            // DUPLICATE NAME
            var nameExists = await ledgerGroupDB.GetAllByName(groupName);

            if (nameExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Group name already in use."
                });
            }
            int userId = _currentUser.UserId;
            //int userId = GetUserId();

            if (userId <= 0)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Unable to identify the authenticated user."
                });
            }

            // GROUP CODE / PARENT LOGIC
            int groupCode;
            int mainGroupId = 0;

            if (request.MainGroup)
            {
                groupCode = await ledgerGroupDB.MaxGroupCode();

                if (groupCode <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Unable to generate group code."
                    });
                }
            }
            else
            {
                if (request.MainGroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Choose main group."
                    });
                }

                mainGroupId = request.MainGroupId;

                var parent = await ledgerGroupDB.SearchByIDs(mainGroupId);

                if (parent == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Selected main group does not exist."
                    });
                }

                if (!parent.IsActive)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Selected main group is inactive."
                    });
                }

                var childGroups =
                    await ledgerGroupDB.GetSubGroupsOrderByGrpCd(mainGroupId);

                if (parent.MainGroup)
                {
                    if (childGroups.Count > 0)
                    {
                        groupCode = childGroups[0].GroupCode + 1;
                    }
                    else
                    {
                        groupCode = parent.GroupCode * 10000;
                    }
                }
                else
                {
                    if (childGroups.Count > 0)
                    {
                        groupCode = childGroups[0].GroupCode + 1;
                    }
                    else
                    {
                        groupCode = parent.GroupCode + 1;
                    }
                }

                // Inherit final account and CR/DR from parent.
                if (!string.IsNullOrWhiteSpace(parent.FinalAccount))
                {
                    finalAccount = parent.FinalAccount.Trim().ToUpperInvariant();
                }

                if (!string.IsNullOrWhiteSpace(parent.CrDr))
                {
                    crDr = parent.CrDr.Trim().ToUpperInvariant();
                }
            }

            // FINAL ACCOUNT (resolved value: request for a main group,
            // parent's value for a sub-group)
            if (!allowedFinalAccounts.Contains(finalAccount))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid Final Account selection. Allowed values are P, T, B or M."
                });
            }

            // CR / DR (resolved value)
            if (crDr != "CR" && crDr != "DR")
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Default type must be CR or DR."
                });
            }

            // DUPLICATE GROUP CODE
            var codeExists =await ledgerGroupDB.GetAllByGroupCode(groupCode);

            if (codeExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Group Code already in use."
                });
            }

            // SEQUENCE NUMBER
            int sequenceNo = request.SequenceNo ?? 0;

            if (sequenceNo <= 0)
            {
                sequenceNo = await ledgerGroupDB.MaxSeqNo();

                if (sequenceNo <= 0)
                {
                    sequenceNo = 1;
                }
            }

            var sequenceExists = await ledgerGroupDB.GetAllBySeqNo(sequenceNo);

            if (sequenceExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Sequence No. already in use."
                });
            }

            // BUILD LEGACY MODEL
            ClsGrpLdg cls = new ClsGrpLdg
            {
                GroupName = groupName,
                SecName = request.SecondName?.Trim() ?? "",
                GroupCode = groupCode,
                GroupNature = finalAccount,
                Order = finalAccount,
                SubLedgeFlag = request.SubLedgerFlag,
                Maingroup = request.MainGroup,
                MGroup = mainGroupId,
                SeqNo = sequenceNo,
                CRDR = crDr,
                Gactive = request.IsActive,
                GDelFlg = false,
                LogCd = userId,
                Utime = DateTime.Now
            };

            int groupId = await ledgerGroupDB.Insert(cls, userId);

            if (groupId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group could not be created."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Ledger group created successfully."
                //data = new
                //{
                //    groupId,
                //    groupCode,
                //    sequenceNo
                //}
            });
        }

        // ============================================================
        // UPDATE
        // ============================================================
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] LedgerGroupRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ledger group ID."
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group data is required."
                });
            }

            var existing = await ledgerGroupDB.SearchByIDs(id);

            if (existing == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group not found."
                });
            }

            // GROUP NAME
            if (string.IsNullOrWhiteSpace(request.GroupName))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Name must not be blank."
                });
            }

            string groupName = request.GroupName.Trim();

            var nameExists =
                await ledgerGroupDB.CheckNameExists(groupName, id);

            if (nameExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Group name already in use."
                });
            }

            // FINAL ACCOUNT / CR-DR: normalised here, validated after the
            // main/sub-group branch (a sub-group inherits both from its parent).
            string finalAccount =
                (request.FinalAccount ?? "").Trim().ToUpperInvariant();

            string[] allowedFinalAccounts = { "P", "T", "B", "M" };

            string crDr = (request.CrDr ?? "").Trim().ToUpperInvariant();

            // MAIN / SUB GROUP
            int groupCode = existing.GroupCode;
            int mainGroupId = 0;

            if (request.MainGroup)
            {
                // Preserve the existing code unless a new one is supplied.
                groupCode = (request.GroupCode ?? 0) > 0
                    ? request.GroupCode!.Value
                    : existing.GroupCode;
            }
            else
            {
                if (request.MainGroupId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Choose main group."
                    });
                }

                mainGroupId = request.MainGroupId;

                if (mainGroupId == id)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "A ledger group cannot be its own parent."
                    });
                }

                var parent = await ledgerGroupDB.SearchByIDs(mainGroupId);

                if (parent == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Selected main group does not exist."
                    });
                }

                if (!parent.IsActive)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Selected main group is inactive."
                    });
                }

                // Prevent selecting one of this group's descendants as its
                // parent. Additional ancestry validation may be needed if
                // your hierarchy supports multiple nested levels.

                if (!string.IsNullOrWhiteSpace(parent.FinalAccount))
                {
                    finalAccount = parent.FinalAccount.Trim()
                        .ToUpperInvariant();
                }

                if (!string.IsNullOrWhiteSpace(parent.CrDr))
                {
                    crDr = parent.CrDr.Trim().ToUpperInvariant();
                }

                groupCode = (request.GroupCode ?? 0) > 0
                    ? request.GroupCode!.Value
                    : existing.GroupCode;
            }

            // FINAL ACCOUNT (resolved value: request for a main group,
            // parent's value for a sub-group)
            if (!allowedFinalAccounts.Contains(finalAccount))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid Final Account selection. Allowed values are P, T, B or M."
                });
            }

            // CR / DR (resolved value)
            if (crDr != "CR" && crDr != "DR")
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Default type must be CR or DR."
                });
            }

            // DUPLICATE GROUP CODE
            var codeExists =
                await ledgerGroupDB.CheckGroupExists(groupCode, id);

            if (codeExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Group Code already in use."
                });
            }

            // SEQUENCE NUMBER
            int sequenceNo = (request.SequenceNo ?? 0) > 0
                ? request.SequenceNo!.Value
                : existing.SequenceNo;

            var sequenceExists =
                await ledgerGroupDB.CheckSeqNoExists(sequenceNo, id);

            if (sequenceExists.Count > 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Sequence No. already in use."
                });
            }

            int userId = GetUserId();

            if (userId <= 0)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Unable to identify the authenticated user."
                });
            }

            ClsGrpLdg cls = new ClsGrpLdg
            {
                GroupId = id,
                GroupName = groupName,
                SecName = request.SecondName?.Trim() ?? "",
                GroupCode = groupCode,
                GroupNature = finalAccount,
                Order = finalAccount,
                SubLedgeFlag = request.SubLedgerFlag,
                Maingroup = request.MainGroup,
                MGroup = mainGroupId,
                SeqNo = sequenceNo,
                CRDR = crDr,
                Gactive = request.IsActive,
                //GDelFlg = existing.GDELFLG,
                LogCd = userId,
                Utime = DateTime.Now
            };

            int rowsAffected = await ledgerGroupDB.Update(cls, userId);

            if (rowsAffected <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group could not be updated."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Ledger group updated successfully."
            });
        }



        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponseWithSuccess), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ledger group ID."
                });
            }

            // 1. Existence (not deleted; GACTIVE not required).
            if (!await ledgerGroupDB.ExistsForDeleteAsync(id))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group not found."
                });
            }

            // 2. System groups referenced by fixed ID in the code.
            if (id is 1 or 4 or 26 or 27)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "System group cannot be deleted."
                });
            }

            // 3. Live sub-groups.
            if (await ledgerGroupDB.HasSubGroupsAsync(id))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group has sub-groups and cannot be deleted."
                });
            }

            // 4. Live accounts under this group.
            if (await ledgerGroupDB.HasAccountsAsync(id))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group has accounts and cannot be deleted."
                });
            }

            // 5. Soft delete.
            var data = await ledgerGroupDB.DeleteAsync(id, _currentUser.UserId);

            if (!data)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Ledger group not found."
                });
            }

            return Ok(new
            {
                status = true,
                message = "Ledger group Deleted successfully."
            });
        }


        // ============================================================
        // ACTIVE LOOKUP
        // ============================================================
        //[HttpGet("lookup")]

        //public async Task<IActionResult> Lookup()
        //{
        //    var groups = await ledgerGroupDB.NameAndId();

        //    var data = groups.Select(group => new
        //    {
        //        id = group.GroupId,
        //        name = group.GroupName
        //    }).ToList();

        //    return Ok(new
        //    {
        //        status = true,
        //        message = "Ledger group lookup retrieved successfully.",
        //        data
        //    });
        //}

        // ============================================================
        // GET SUB GROUPS
        // ============================================================
        //[HttpGet("{id:int}/sub-groups")]
        //public async Task<IActionResult> GetSubGroups(int id)
        //{
        //    if (id <= 0)
        //    {
        //        return BadRequest(new
        //        {
        //            status = false,
        //            message = "Invalid main group ID."
        //        });
        //    }

        //    var parent = await ledgerGroupDB.SearchByID(id);

        //    if (parent == null)
        //    {
        //        return NotFound(new
        //        {
        //            status = false,
        //            message = "Main group not found."
        //        });
        //    }

        //    var data = await ledgerGroupDB.GetSubGroups(id);

        //    return Ok(new
        //    {
        //        status = true,
        //        message = "Sub groups retrieved successfully.",
        //        data
        //    });
        //}

        // ============================================================
        // CUSTOMER / SUPPLIER LEDGER GROUPS
        // ============================================================
        [HttpGet("customer-supplier")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<LedgerGroupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomerSupplierLedgerGroups(
            [FromQuery] string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(
                    ApiResponseWithError.Failure(
                        "Type is required. Use C for Customer or S for Supplier."));
            }

            if (!type.Equals("C", StringComparison.OrdinalIgnoreCase) &&
                !type.Equals("S", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    ApiResponseWithError.Failure(
                        "Invalid type. Use C for Customer or S for Supplier."));
            }

            var data =
                await ledgerGroupDB.CustOrSupLedgerGroup(type);

            return Ok(
                ApiResponseWithoutPagination<object>.Success(
                    "Customer/Supplier ledger groups retrieved successfully.",
                    data));
        }
        // ============================================================
        // FINAL ACCOUNTS LOOKUP
        // ============================================================
        [HttpGet("lookups/final-accounts")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<FinalAccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetFinalAccountsLookup()
        {
            var data = ledgerGroupDB.GetFinalAccountsLookup();

            return Ok(new
            {
                status = true,
                message = "Final accounts lookup retrieved successfully.",
                data=data
                //data = data.Select(item => new
                //{
                //    id = item.Code,
                //    name = item.FinalAccount
                //}).ToList()
            });
        }

        // ============================================================
        // USER ID FROM JWT
        // ============================================================
        private int GetUserId()
        {
            string? value =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = User.FindFirstValue("userId");
            }

            return int.TryParse(value, out int userId)
                ? userId
                : 0;
        }
    }
}


//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using QuickBalance.API.CLS;
//using QuickBalance.API.DB;
//using QuickBalance.API.DTOs;
//using System.Data;
//using System.Security.Claims;
//using System.Text.RegularExpressions;

//namespace QuickBalance.API.Controllers.Master
//{
//    [ApiController]
//    [Route("api/v1/ledger-groups")]
//    [Authorize]
//    public class LedgerGroupController : ControllerBase
//    {
//        private readonly LedgerGroupDB ledgerGroupDB;

//        public LedgerGroupController(LedgerGroupDB ledgerGroupDB)
//        {
//            this.ledgerGroupDB = ledgerGroupDB;
//        }

//        // ============================================================
//        // GET ALL - PAGINATED
//        // ============================================================
//        //[HttpGet]
//        //public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
//        //{
//        //    request.Normalize();

//        //    DataTable dt = await ledgerGroupDB.GetAll(request.Search);

//        //    int totalRecords = dt.Rows.Count;

//        //    int totalPages =(int)Math.Ceiling(
//        //            totalRecords / (double)request.PageSize
//        //        );

//        //    var data = dt.AsEnumerable()
//        //        .Skip(request.Offset)
//        //        .Take(request.PageSize)
//        //        .Select(ConvertRow)
//        //        .ToList();

//        //    return Ok(new
//        //    {
//        //        status = true,
//        //        message = "Ledger group list retrieved successfully.",
//        //        data,
//        //        pagination = new
//        //        {
//        //            page = request.Page,
//        //            pageSize = request.PageSize,
//        //            totalRecords,
//        //            totalPages
//        //        }
//        //    });
//        //}


//        [HttpGet]
//        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
//        {
//            request.Normalize();

//            var result = await ledgerGroupDB.GetAll(request.Search,request.Page,request.PageSize);

//            int totalPages = (int)Math.Ceiling(result.TotalRecords / (double)request.PageSize);

//            return Ok(new
//            {
//                status = true,
//                message = "Ledger group list retrieved successfully.",
//                data = result.Data,
//                pagination = new
//                {
//                    page = request.Page,
//                    pageSize = request.PageSize,
//                    totalRecords = result.TotalRecords,
//                    totalPages
//                }
//            });
//        }
//        // ============================================================
//        // GET BY ID
//        // ============================================================
//        [HttpGet("{id:int}")]
//        public async Task<IActionResult> GetById(int id)
//        {
//            if (id <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid ledger group ID."
//                });
//            }

//            DataTable dt = await ledgerGroupDB.SearchByID(id);

//            if (dt.Rows.Count == 0)
//            {
//                return NotFound(new
//                {
//                    status = false,
//                    message = "Ledger group not found."
//                });
//            }

//            return Ok(new
//            {
//                status = true,
//                message = "Ledger group retrieved successfully.",
//                data = ConvertRow(dt.Rows[0])
//            });
//        }

//        // ============================================================
//        // CREATE
//        // ============================================================
//        [HttpPost]
//        public async Task<IActionResult> Create(
//            [FromBody] LedgerGroupRequest request)
//        {
//            if (request == null)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Ledger group data is required."
//                });
//            }

//            // --------------------------------------------------------
//            // GROUP NAME
//            // --------------------------------------------------------
//            if (string.IsNullOrWhiteSpace(request.GroupName))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Name must not be blank."
//                });
//            }

//            string groupName = request.GroupName.Trim();

//            // --------------------------------------------------------
//            // FINAL ACCOUNT
//            // --------------------------------------------------------
//            string finalAccount =
//                (request.FinalAccount ?? "").Trim().ToUpper();

//            string[] allowedFinalAccounts =
//            {
//                "P",
//                "T",
//                "B",
//                "M"
//            };

//            if (!allowedFinalAccounts.Contains(finalAccount))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid Final Account selection. Allowed values are P, T, B or M."
//                });
//            }

//            // --------------------------------------------------------
//            // CR / DR
//            // --------------------------------------------------------
//            string crDr =
//                (request.CrDr ?? "").Trim();

//            if (string.IsNullOrWhiteSpace(crDr))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid Default Type Selection."
//                });
//            }

//            crDr = crDr.ToUpper();

//            if (crDr != "CR" && crDr != "DR")
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Default type must be CR or DR."
//                });
//            }

//            // --------------------------------------------------------
//            // DUPLICATE NAME
//            // --------------------------------------------------------
//            DataTable nameExists =
//                await ledgerGroupDB.GetAllByName(groupName);

//            if (nameExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Group name already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // GET USER ID
//            // --------------------------------------------------------
//            int userId = GetUserId();

//            // --------------------------------------------------------
//            // GROUP CODE / PARENT LOGIC
//            // --------------------------------------------------------
//            int groupCode;
//            int mainGroupId = 0;

//            if (request.MainGroup)
//            {
//                // Main group:
//                // Old application generated the next group code.
//                groupCode =
//                    await ledgerGroupDB.MaxGroupCode();

//                if (groupCode <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Unable to generate group code."
//                    });
//                }
//            }
//            else
//            {
//                // ----------------------------------------------------
//                // SUB GROUP REQUIRES PARENT
//                // ----------------------------------------------------
//                if (request.MainGroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Choose main group."
//                    });
//                }

//                mainGroupId = request.MainGroupId;

//                DataTable parentTable =
//                    await ledgerGroupDB.SearchByID(mainGroupId);

//                if (parentTable.Rows.Count == 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Selected main group does not exist."
//                    });
//                }

//                DataRow parent =
//                    parentTable.Rows[0];

//                bool parentIsMain =
//                    Convert.ToBoolean(parent["MAINGROUP"]);

//                // ----------------------------------------------------
//                // OLD APPLICATION CODE GENERATION
//                // ----------------------------------------------------
//                DataTable childGroups =
//                    await ledgerGroupDB.GetSubGroupsOrderByGrpCd(
//                        mainGroupId);

//                if (parentIsMain)
//                {
//                    if (childGroups.Rows.Count > 0)
//                    {
//                        // Existing subgroups.
//                        // Use latest group code + 1.
//                        int latestCode =
//                            Convert.ToInt32(
//                                childGroups.Rows[0]["GROUPCODE"]);

//                        groupCode = latestCode + 1;
//                    }
//                    else
//                    {
//                        // First subgroup under a main group.
//                        int parentCode =
//                            Convert.ToInt32(
//                                parent["GROUPCODE"]);

//                        groupCode =
//                            parentCode * 10000;
//                    }
//                }
//                else
//                {
//                    // Parent itself is a subgroup.
//                    if (childGroups.Rows.Count > 0)
//                    {
//                        int latestCode =
//                            Convert.ToInt32(
//                                childGroups.Rows[0]["GROUPCODE"]);

//                        groupCode = latestCode + 1;
//                    }
//                    else
//                    {
//                        int parentCode =
//                            Convert.ToInt32(
//                                parent["GROUPCODE"]);

//                        groupCode =
//                            parentCode + 1;
//                    }
//                }

//                // ----------------------------------------------------
//                // FINAL ACCOUNT IS INHERITED FROM PARENT
//                // ----------------------------------------------------
//                finalAccount =
//                    Convert.ToString(
//                        parent["GROUPNATURE"]) ?? finalAccount;

//                finalAccount =
//                    finalAccount.Trim().ToUpper();

//                // ----------------------------------------------------
//                // CR/DR IS ALSO INHERITED FROM PARENT
//                // ----------------------------------------------------
//                string parentCrDr =
//                    Convert.ToString(parent["CRDR"]) ?? "";

//                if (!string.IsNullOrWhiteSpace(parentCrDr))
//                {
//                    crDr = parentCrDr.Trim().ToUpper();
//                }
//            }

//            // --------------------------------------------------------
//            // GROUP CODE DUPLICATE CHECK
//            // --------------------------------------------------------
//            DataTable codeExists =
//                await ledgerGroupDB.GetAllByGroupCode(groupCode);

//            if (codeExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Group Code already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // SEQUENCE NUMBER
//            // --------------------------------------------------------
//            int sequenceNo = request.SequenceNo;

//            if (sequenceNo <= 0)
//            {
//                sequenceNo =
//                    await ledgerGroupDB.MaxSeqNo();

//                if (sequenceNo <= 0)
//                {
//                    sequenceNo = 1;
//                }
//            }

//            DataTable seqExists =
//                await ledgerGroupDB.GetAllBySeqNo(sequenceNo);

//            if (seqExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Sequence No. already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // BUILD LEGACY CLASS
//            // --------------------------------------------------------
//            ClsGrpLdg cls = new ClsGrpLdg
//            {
//                GroupName = groupName,
//                SecName = request.SecondName?.Trim() ?? "",
//                GroupCode = groupCode,
//                GroupNature = finalAccount,
//                Order = finalAccount,
//                SubLedgeFlag = request.SubLedgerFlag,
//                Maingroup = request.MainGroup,
//                MGroup = mainGroupId,
//                SeqNo = sequenceNo,
//                CRDR = crDr,
//                Gactive = request.IsActive,
//                GDelFlg = false,
//                LogCd = userId,
//                Utime = DateTime.Now
//            };

//            int groupId =
//                await ledgerGroupDB.Insert(
//                    cls,
//                    userId);

//            if (groupId <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Ledger group could not be created."
//                });
//            }

//            return Ok(new
//            {
//                status = true,
//                message = "Ledger group created successfully.",
//                data = new
//                {
//                    groupId,
//                    groupCode,
//                    sequenceNo
//                }
//            });
//        }

//        // ============================================================
//        // UPDATE
//        // ============================================================
//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> Update(
//            int id,
//            [FromBody] LedgerGroupRequest request)
//        {
//            if (id <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid ledger group ID."
//                });
//            }

//            if (request == null)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Ledger group data is required."
//                });
//            }

//            // --------------------------------------------------------
//            // EXISTING GROUP
//            // --------------------------------------------------------
//            //DataTable existing =await ledgerGroupDB.SearchByIDs(id);
//            var result = await ledgerGroupDB.SearchByIDs(id);


//            //if (existing.Rows.Count == 0)
//            //{
//            //    return NotFound(new
//            //    {
//            //        status = false,
//            //        message = "Ledger group not found."
//            //    });
//            //}

//            //DataRow current =
//            //    existing.Rows[0];

//            // --------------------------------------------------------
//            // GROUP NAME
//            // --------------------------------------------------------
//            if (string.IsNullOrWhiteSpace(request.GroupName))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Name must not be blank."
//                });
//            }

//            string groupName =
//                request.GroupName.Trim();

//            DataTable nameExists =
//                await ledgerGroupDB.CheckNameExists(
//                    groupName,
//                    id);

//            if (nameExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Group name already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // FINAL ACCOUNT
//            // --------------------------------------------------------
//            string finalAccount =
//                (request.FinalAccount ?? "")
//                .Trim()
//                .ToUpper();

//            string[] allowedFinalAccounts =
//            {
//                "P",
//                "T",
//                "B",
//                "M"
//            };

//            if (!allowedFinalAccounts.Contains(finalAccount))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid Final Account selection. Allowed values are P, T, B or M."
//                });
//            }

//            // --------------------------------------------------------
//            // CR / DR
//            // --------------------------------------------------------
//            string crDr =
//                (request.CrDr ?? "")
//                .Trim()
//                .ToUpper();

//            if (crDr != "CR" && crDr != "DR")
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Default type must be CR or DR."
//                });
//            }

//            // --------------------------------------------------------
//            // MAIN / SUB GROUP
//            // --------------------------------------------------------
//            int groupCode = Convert.ToInt32(result.GroupCode);
//            //Convert.ToInt32(current["GROUPCODE"]);

//            int mainGroupId = 0;

//            if (request.MainGroup)
//            {
//                // Main group retains the submitted/current group code.
//                // We don't regenerate it during update.
//                groupCode =
//                    request.GroupCode > 0
//                        ? request.GroupCode
//                        : groupCode;
//            }
//            else
//            {
//                if (request.MainGroupId <= 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Choose main group."
//                    });
//                }

//                mainGroupId =
//                    request.MainGroupId;

//                DataTable parentTable =
//                    await ledgerGroupDB.SearchByID(
//                        mainGroupId);

//                if (parentTable.Rows.Count == 0)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "Selected main group does not exist."
//                    });
//                }

//                DataRow parent =
//                    parentTable.Rows[0];

//                // A group cannot be its own parent.
//                if (mainGroupId == id)
//                {
//                    return BadRequest(new
//                    {
//                        status = false,
//                        message = "A ledger group cannot be its own parent."
//                    });
//                }

//                // Legacy behavior: subgroup inherits these values.
//                finalAccount =
//                    Convert.ToString(
//                        parent["GROUPNATURE"]) ?? finalAccount;

//                finalAccount =
//                    finalAccount.Trim().ToUpper();

//                string parentCrDr =
//                    Convert.ToString(
//                        parent["CRDR"]) ?? "";

//                if (!string.IsNullOrWhiteSpace(parentCrDr))
//                {
//                    crDr =
//                        parentCrDr.Trim().ToUpper();
//                }

//                // Keep existing subgroup code during update unless
//                // a new valid code was explicitly supplied.
//                groupCode =
//                    request.GroupCode > 0
//                        ? request.GroupCode
//                        : groupCode;
//            }

//            // --------------------------------------------------------
//            // GROUP CODE DUPLICATE
//            // --------------------------------------------------------
//            DataTable codeExists =
//                await ledgerGroupDB.CheckGroupExists(
//                    groupCode,
//                    id);

//            if (codeExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Group Code already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // SEQUENCE NUMBER
//            // --------------------------------------------------------
//            int sequenceNo =
//                request.SequenceNo > 0
//                    ? request.SequenceNo
//                    : Convert.ToInt32(result.SequenceNo);   //Convert.ToInt32(current["SEQNO"]);

//            DataTable seqExists =
//                await ledgerGroupDB.CheckSeqNoExists(
//                    sequenceNo,
//                    id);

//            if (seqExists.Rows.Count > 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Sequence No. already in use."
//                });
//            }

//            // --------------------------------------------------------
//            // UPDATE CLASS
//            // --------------------------------------------------------
//            int userId = GetUserId();

//            ClsGrpLdg cls = new ClsGrpLdg
//            {
//                GroupId = id,
//                GroupName = groupName,
//                SecName = request.SecondName?.Trim() ?? "",
//                GroupCode = groupCode,
//                GroupNature = finalAccount,
//                Order = finalAccount,
//                SubLedgeFlag = request.SubLedgerFlag,
//                Maingroup = request.MainGroup,
//                MGroup = mainGroupId,
//                SeqNo = sequenceNo,
//                CRDR = crDr,
//                Gactive = request.IsActive,
//                GDelFlg = result.GDELFLG, //Convert.ToBoolean(current["GDELFLG"]),
//                LogCd = userId,
//                Utime = DateTime.Now
//            };

//            int results =await ledgerGroupDB.Update(cls,userId);

//            if (results <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Ledger group could not be updated."
//                });
//            }

//            return Ok(new
//            {
//                status = true,
//                message = "Ledger group updated successfully."
//            });
//        }

//        // ============================================================
//        // ACTIVE LOOKUP
//        // ============================================================
//        [HttpGet("lookup")]
//        public async Task<IActionResult> Lookup()
//        {
//            DataTable dt =
//                await ledgerGroupDB.NameAndId();

//            var data = dt.AsEnumerable()
//                .Select(row => new
//                {
//                    id = Convert.ToInt32(row["GROUPID"]),
//                    name =
//                        Convert.ToString(
//                            row["GROUPNAME"]) ?? ""
//                })
//                .ToList();

//            return Ok(new
//            {
//                status = true,
//                message = "Ledger group lookup retrieved successfully.",
//                data
//            });
//        }

//        // ============================================================
//        // GET SUB GROUPS
//        // ============================================================
//        [HttpGet("{id:int}/sub-groups")]
//        public async Task<IActionResult> GetSubGroups(
//            int id)
//        {
//            if (id <= 0)
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid main group ID."
//                });
//            }

//            DataTable parent =
//                await ledgerGroupDB.SearchByID(id);

//            if (parent.Rows.Count == 0)
//            {
//                return NotFound(new
//                {
//                    status = false,
//                    message = "Main group not found."
//                });
//            }

//            DataTable dt =
//                await ledgerGroupDB.GetSubGroups(id);

//            var data = dt.AsEnumerable()
//                .Select(ConvertRow)
//                .ToList();

//            return Ok(new
//            {
//                status = true,
//                message = "Sub groups retrieved successfully.",
//                data
//            });
//        }

//        // ============================================================
//        // GET CUSTOMER / SUPPLIER LEDGER GROUPS
//        // ============================================================
//        [HttpGet("customer-supplier")]
//        public async Task<IActionResult> GetCustomerSupplierLedgerGroups(
//            [FromQuery] string type)
//        {
//            if (string.IsNullOrWhiteSpace(type))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Type is required. Use C for Customer or S for Supplier."
//                });
//            }

//            if (!type.Equals("C", StringComparison.OrdinalIgnoreCase) &&
//                !type.Equals("S", StringComparison.OrdinalIgnoreCase))
//            {
//                return BadRequest(new
//                {
//                    status = false,
//                    message = "Invalid type. Use C for Customer or S for Supplier."
//                });
//            }

//            // IMPORTANT:
//            // Use the actual injected field name from this controller.
//            DataTable table =
//                await ledgerGroupDB.CustOrSupLedgerGroup(type);

//            // IMPORTANT:
//            // CustOrSupLedgerGroup() only returns:
//            // GROUPID, GROUPNAME, GROUPCODE, GROUPNATURE
//            //
//            // Therefore DO NOT use ConvertRow() here.
//            var data = table.AsEnumerable()
//                .Select(row => new
//                {
//                    groupId =
//                        Convert.ToInt32(row["GROUPID"]),

//                    groupName =
//                        Convert.ToString(
//                            row["GROUPNAME"]) ?? "",

//                    groupCode =
//                        Convert.ToInt32(
//                            row["GROUPCODE"]),

//                    groupNature =
//                        Convert.ToString(
//                            row["GROUPNATURE"]) ?? ""
//                })
//                .ToList();

//            return Ok(new
//            {
//                status = true,
//                message = "Customer/Supplier ledger groups retrieved successfully.",
//                data
//            });
//        }

//        // ============================================================
//        // CONVERT DATAROW
//        // ============================================================
//        private static object ConvertRow(DataRow row)
//        {
//            return new
//            {
//                groupId =
//                    Convert.ToInt32(row["GROUPID"]),

//                groupName =
//                    Convert.ToString(row["GROUPNAME"]) ?? "",

//                secondName =
//                    Convert.ToString(row["SECNAME"]) ?? "",

//                groupCode =
//                    Convert.ToInt32(row["GROUPCODE"]),

//                finalAccount =
//                    Convert.ToString(row["GROUPNATURE"]) ?? "",

//                subLedgerFlag =
//                    Convert.ToBoolean(row["SUBLEDGFLAG"]),

//                mainGroup =
//                    Convert.ToBoolean(row["MAINGROUP"]),

//                mainGroupId =
//                    Convert.ToInt32(row["MGROUP"]),

//                sequenceNo =
//                    Convert.ToInt32(row["SEQNO"]),

//                crDr =
//                    Convert.ToString(row["CRDR"]) ?? "",

//                isActive =
//                    Convert.ToBoolean(row["GACTIVE"]),

//                approvalStatus =
//                    Convert.ToString(row["APRVLST"]) ?? ""
//            };
//        }

//        // ============================================================
//        // USER ID FROM JWT
//        // ============================================================
//        private int GetUserId()
//        {
//            string? value =
//                User.FindFirstValue(
//                    ClaimTypes.NameIdentifier);

//            if (string.IsNullOrWhiteSpace(value))
//            {
//                value =
//                    User.FindFirstValue("userId");
//            }

//            if (int.TryParse(value, out int userId))
//            {
//                return userId;
//            }

//            return 0;
//        }

//        // ============================================================
//        // FINAL ACCOUNTS LOOKUP
//        // ============================================================
//        [HttpGet("lookups/final-accounts")]
//        public async Task<IActionResult> GetFinalAccountsLookup()
//        {
//            DataTable dt = await ledgerGroupDB.GetFinalAccountsLookup();

//            var data = dt.AsEnumerable()
//                .Select(row => new
//                {
//                    id = Convert.ToString(row["CODE"]) ?? "",
//                    name = Convert.ToString(row["FINALACC"]) ?? ""
//                })
//                .ToList();

//            return Ok(new
//            {
//                status = true,
//                message = "Final accounts lookup retrieved successfully.",
//                data
//            });
//        }
//    }
//}