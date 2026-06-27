using AIAnalyzer.Models;
using AIAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyzer.Controllers
{
    public class SettingsController : Controller
    {
        private readonly LlmSettingsService _llmService;

        public SettingsController(LlmSettingsService llmService)
        {
            _llmService = llmService;
        }

        // Возвращает список для отображения в интерфейсе
        [HttpGet]
        public IActionResult GetProviders()
        {
            return Json(_llmService.GetAllProviders());
        }

        // Принимает данные с формы добавления
        [HttpPost]
        public IActionResult AddProvider([FromBody] LlmProvider provider)
        {
            _llmService.AddProvider(provider);
            return Ok();
        }

        // Удаляет по ID
        [HttpDelete]
        public IActionResult DeleteProvider(string id)
        {
            _llmService.DeleteProvider(id);
            return Ok();
        }
    }
}
