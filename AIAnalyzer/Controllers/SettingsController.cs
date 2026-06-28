using AIAnalyzer.Models;
using AIAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyzer.Controllers
{
    [Route("Settings")]
    public class SettingsController : Controller
    {
        [HttpGet("")] // Это метод для получения страницы (Index)
        public IActionResult Index()
        {
            return View();
        }
        private readonly LlmSettingsService _llmService;

        public SettingsController(LlmSettingsService llmService)
        {
            _llmService = llmService;
        }

        // Возвращает список для отображения в интерфейсе
        [HttpGet("GetProviders")]
        public IActionResult GetProviders()
        {
            return Json(_llmService.GetAllProviders());
        }

        // Принимает данные с формы добавления
        [HttpPost("AddProvider")]
        public IActionResult AddProvider([FromBody] LlmProvider provider)
        {
            _llmService.AddProvider(provider);
            return Ok();
        }

        // Удаляет по ID
        [HttpDelete("DeleteProvider")]
        public IActionResult DeleteProvider(string id)
        {
            _llmService.DeleteProvider(id);
            return Ok();
        }
    }
}
