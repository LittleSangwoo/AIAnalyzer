using Microsoft.AspNetCore.Mvc;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Services;
using System.Text.Json.Serialization;

namespace AIAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("recommendation")]
        public async Task<IActionResult> GetRecommendation([FromBody] AiRecommendationRequest request)
        {
            if (request?.Questions == null || !request.Questions.Any())
                return BadRequest("Нет вопросов для анализа.");

            try
            {
                string aiResponse = await _aiService.GenerateRecommendationAsync(
                    request.Questions, request.PromptType, request.ModelProvider, request.ApiKey);
                return Ok(aiResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("custom")]
        public async Task<IActionResult> Custom([FromBody] CustomPromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt)) return BadRequest("Текст промпта пуст.");

            try
            {
                string aiResponse = await _aiService.ProcessCustomPromptAsync(
                    request.Prompt, request.ModelProvider, request.ApiKey);
                return Ok(aiResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class AiRecommendationRequest
    {
        [JsonPropertyName("redZoneQuestions")]
        public List<QuestionStatDto> Questions { get; set; }
        public string? PromptType { get; set; }
        public string? ModelProvider { get; set; }
        public string? ApiKey { get; set; }
    }

    public class CustomPromptRequest
    {
        public string Prompt { get; set; }
        public string ModelProvider { get; set; }
        public string ApiKey { get; set; }
    }
}