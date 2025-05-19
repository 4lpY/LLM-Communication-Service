using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LLMCommService.Interfaces;
using LLMCommService.Models;
using Microsoft.AspNetCore.Mvc;

namespace LLMCommService.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class LMMCommController : ControllerBase
    {
        private readonly IClaudeService _claudeService;

        public LMMCommController(IClaudeService claudeService)
        {
            _claudeService = claudeService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] LLMRequest request)
        {
            if(string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest();
            }

            try
            {
                var result = await _claudeService.ExtractMessageAsync(request.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error occured: {ex}");
            }
        }
    }
}