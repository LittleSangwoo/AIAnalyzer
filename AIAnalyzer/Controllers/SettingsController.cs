using AIAnalyzer.Models;
using AIAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyzer.Controllers
{
    [Route("Settings")]
    public class SettingsController : Controller
    {
        [HttpGet("")] 
        public IActionResult Index()
        {
            return View();
        }
        private readonly LlmSettingsService _llmService;

        public SettingsController(LlmSettingsService llmService)
        {
            _llmService = llmService;
        }

        [HttpGet("GetProviders")]
        public IActionResult GetProviders()
        {
            return Json(_llmService.GetAllProviders());
        }

        [HttpPost("AddProvider")]
        public IActionResult AddProvider([FromBody] LlmProvider provider)
        {
            _llmService.AddProvider(provider);
            return Ok();
        }

        [HttpDelete("DeleteProvider")]
        public IActionResult DeleteProvider(string id)
        {
            _llmService.DeleteProvider(id);
            return Ok();
        }
    }
}
