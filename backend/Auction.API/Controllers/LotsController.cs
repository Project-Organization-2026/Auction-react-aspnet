using Auction.BLL.DTOs.Lots;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Superpower.Model;

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
                bool deleted = await _lotsService.DeleteLotAsync(id);
                if (deleted)
                {
                    return Ok();
                }
                return StatusCode(500, "An error occurred while deleting the lot.");
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
