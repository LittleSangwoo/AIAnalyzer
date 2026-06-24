using Microsoft.AspNetCore.Mvc;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;
using AIAnalyzer.Services;
using System.Text.Json;

namespace AIAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IReportService _reportService;

        public AiController(IAiService aiService, IReportService reportService)
        {
            _aiService = aiService;
            _reportService = reportService;
        }


        // --- ОБНОВЛЕННЫЙ метод анализа: принимает список вопросов, пришедший с фронта ---
        [HttpPost("recommendation")]
        public async Task<IActionResult> GetRecommendation([FromBody] AiRecommendationRequest request)
        {
            try
            {
                string aiResponse = await _aiService.GenerateRecommendationAsync(
                    request.Questions, request.PromptType, request.ModelProvider);
                return Ok(aiResponse);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // Вернет 400 Bad Request с текстом ошибки
            }
        }

        // --- НОВЫЙ метод для произвольных текстовых промптов ---
        [HttpPost("custom")]
        public async Task<IActionResult> Custom([FromBody] CustomPromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Текст промпта пуст.");
            }

            string aiResponse = await _aiService.ProcessCustomPromptAsync(request.Prompt, request.ModelProvider);
            return Ok(aiResponse);
        }

        [HttpPost("export")]
        public IActionResult ExportReport([FromBody] ExportRequest request)
        {
            if (request?.Questions == null) return BadRequest("Нет данных.");
            byte[] fileBytes = _reportService.GenerateExcelReport(request.Questions, request.AiRecommendation);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AI_Analyzer_Report.xlsx");
        }
    }

    // DTO для рекомендаций
    public class AiRecommendationRequest
    {
        public List<QuestionStatDto> Questions { get; set; }
        public string PromptType { get; set; }
        public string ModelProvider { get; set; }
    }

    // DTO для кастомных запросов
    public class CustomPromptRequest
    {
        public string Prompt { get; set; }
        public string ModelProvider { get; set; }
    }

    public class ExportRequest
    {
        public List<QuestionStatDto> Questions { get; set; }
        public string AiRecommendation { get; set; }
    }
}