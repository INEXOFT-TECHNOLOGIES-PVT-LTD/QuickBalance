using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using QuickBalance.API.DTOs.Currencies;

namespace QuickBalance.API.Controllers.Master
{
    [ApiController]
    [Tags("07. Lookups")]
    [Route("api/v1")]
    [Authorize]
    public sealed class CommonController : ControllerBase
    {
        // GET: api/vat-types
        [HttpGet("vat-types")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<List<DropdownOption>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public Task<ApiResponseWithoutPagination<List<DropdownOption>>> GetVatTypes()
        {
            var vatTypes = new List<DropdownOption>
            {
                new() { Id = "Standard VAT", Name = "Standard VAT" },
                new() { Id = "Multi-rate VAT", Name = "Multi-rate VAT" },
                new() { Id = "Zero-rated VAT", Name = "Zero-rated VAT" },
                new() { Id = "Reverse-charge VAT", Name = "Reverse-charge VAT" },
                new() { Id = "Differential VAT", Name = "Differential VAT" },
                new() { Id = "Destination-based VAT", Name = "Destination-based VAT" },
                new() { Id = "Small Business VAT", Name = "Small Business VAT" }
            };
            var response = ApiResponseWithoutPagination<List<DropdownOption>>
            .Success("VAT types retrieved successfully.", vatTypes);

            return Task.FromResult(response);
            //return Task.FromResult(vatTypes);
        }

        [HttpGet("einvoice-id-types")]
        [ProducesResponseType(typeof(ApiResponseWithoutPagination<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseWithError), StatusCodes.Status500InternalServerError)]
        public Task<ApiResponseWithoutPagination<List<string>>> GetIdTypes()
        {
            var data = new List<string>
            {
                "CRN", "MOM", "MLS", "700","SAG", "OTH", "NAT", "IQA", "PAS", "GCC"
            };

            var response = ApiResponseWithoutPagination<List<string>>
             .Success("E-invoice ID types retrieved successfully.", data);

            return Task.FromResult(response);

        }

    }


}