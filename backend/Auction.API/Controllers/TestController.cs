using Auction.BLL.DTOs.Lots;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly LotsService _lotsService;

        public TestController(LotsService lotsService)
        {
            _lotsService = lotsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLots()
        {
            var lots = await _lotsService.GetAllAsync();
            return Ok(lots);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateLot([FromBody] UpdateLotDto updateLotDto, int id)
        {
            var updatedLot = await _lotsService.UpdateLotAsync(updateLotDto, id);
            if (updatedLot == null)
            {
                return NotFound();
            }
            return Ok(updatedLot);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLot(int id)
        {
            var result = await _lotsService.DeleteLotAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateLot([FromBody] CreateLotDto createLotDto)
        {
            var createdLot = await _lotsService.CreateLotAsync(createLotDto);
            return Ok(createdLot);
        }
    }
}
