using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
       
        private readonly ISettingService _settingService;
        
        public UserController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet("settings/{id}")]
        public async Task<IActionResult> GetSettings([FromRoute] Guid id)
        {
            var result = await _settingService.GetSettingsByUserIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(result);
        }

        [HttpPut("update/settings/{id}")]
        public async Task<IActionResult> UpdateSettings([FromRoute] Guid id, [FromBody] SettingDto settings)
        {
            var result = await _settingService.UpdateSettingAsync(id, settings);
            return Ok(result);
        }
    }   
}
