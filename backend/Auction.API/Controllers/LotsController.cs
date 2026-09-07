using Auction.BLL.DTOs.Lots;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LotsController : ControllerBase
    {
        private readonly LotsService _lotsService;

        public LotsController(LotsService lotsService)
        {
            _lotsService = lotsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLots()
        {
            var lots = await _lotsService.GetAllAsync();
            return Ok(lots);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLot(
            [FromRoute] int id,
            [FromBody] UpdateLotDto updateLotDto)
        {
            try
            {
                var updatedLot = await _lotsService.UpdateLotAsync(updateLotDto, id);
                return Ok(updatedLot);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLot([FromRoute] int id)
        {
            try
            {
                var result = await _lotsService.DeleteLotAsync(id);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateLot([FromBody] CreateLotDto createLotDto)
        {
            try
            {
                var createdLot = await _lotsService.CreateLotAsync(createLotDto);
                return Ok(createdLot);

            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
