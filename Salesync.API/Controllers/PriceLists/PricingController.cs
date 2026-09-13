using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.PriceLists.Interfaces;

namespace Salesync.API.Controllers.PriceLists
{
    [ApiController]
    [Route("api/pricing")]
    [Authorize]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(
            IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpGet("customer/{customerId:int}/product/{productId:int}")]
        public async Task<IActionResult> GetPrice(
            int customerId,
            int productId)
        {
            var unitPrice =
                await _pricingService.GetUnitPriceAsync(
                    customerId,
                    productId);

            return Ok(new
            {
                customerId,
                productId,
                unitPrice
            });
        }
    }
}