using Microsoft.AspNetCore.Mvc;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;
using AIAnalyzer.Services;

namespace AIAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Эндпоинты будут доступны по адресу: api/ai
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IReportService _reportService; // Сервис отчетов через Dependency Injection

        public AiController(IAiService aiService, IReportService reportService)
        {
            _aiService = aiService;
            _reportService = reportService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeQuestions([FromBody] AnalysisRequest request)
        {
            if (request?.Questions == null || !request.Questions.Any())
            {
                return BadRequest("Нет данных для анализа.");
            }

            // Фильтруем только красную зону, как договаривались в плане
            var redZoneQuestions = request.Questions
                .Where(q => q.Zone == ErrorZone.Red || q.ErrorsCount >= 6)
                .ToList();

            if (!redZoneQuestions.Any())
            {
                // Возвращаем JSON-объект с полем text, чтобы фронтенд (JS) не сломался
                return Ok(new { text = "Красная зона пуста. Анализ ИИ не требуется, все студенты справились отлично!" });
            }

            // Вызываем ИИ сервис
            string aiResponse = await _aiService.GenerateRecommendationAsync(redZoneQuestions, request.PromptType, request.ModelProvider);

            return Ok(new { text = aiResponse });
        }

        // Метод экспорта Excel, чтобы кнопка скачивания на фронтенде работала
        [HttpPost("export")]
        public IActionResult ExportReport([FromBody] ExportRequest request)
        {
            if (request?.Questions == null)
            {
                return BadRequest("Нет данных для формирования отчета.");
            }

            // Генерируем массив байтов Excel-файла через ClosedXML
            byte[] fileBytes = _reportService.GenerateExcelReport(request.Questions, request.AiRecommendation);

            // Возвращаем файл пользователю для скачивания в браузере
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AI_Analyzer_Report.xlsx"
            );
        }
    }

    // Вспомогательный класс для приема запроса на анализ
    public class AnalysisRequest
    {
        public List<QuestionStatDto> Questions { get; set; }
        public string PromptType { get; set; }     // "compare", "critical", "report"
        public string ModelProvider { get; set; }  // "deepseek" или "gigachat"
    }

    // Вспомогательный класс для приема запроса на экспорт Excel
    public class ExportRequest
    {
        public List<QuestionStatDto> Questions { get; set; }
        public string AiRecommendation { get; set; }
    }
}