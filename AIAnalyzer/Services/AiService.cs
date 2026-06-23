using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using AIAnalyzer.Models.DTOs;

namespace AIAnalyzer.Services
{
    // ИНТЕРФЕЙС (объявлен здесь же, в верху файла)
    public interface IAiService
    {
        // Теперь здесь честно прописаны 3 параметра, включая modelProvider!
        Task<string> GenerateRecommendationAsync(List<QuestionStatDto> redZoneQuestions, string promptType, string modelProvider);
    }

    // РЕАЛИЗАЦИЯ СЕРВИСА
    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Класс успешно реализует интерфейс, так как параметры (3 штуки) совпадают!
        public async Task<string> GenerateRecommendationAsync(List<QuestionStatDto> redZoneQuestions, string promptType, string modelProvider)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Список проблемных вопросов (Красная зона):");
            foreach (var q in redZoneQuestions)
            {
                sb.AppendLine($"- ID: {q.QuestionId} | Вопрос: \"{q.QuestionText}\" | Ошибок: {q.ErrorsCount}");
            }

            string systemPrompt = promptType.ToLower() switch
            {
                "compare" => "Ты — эксперт-аналитик тестов. Сравни эти вопросы между собой. Найди общую закономерность: почему студенты ошибаются именно в них?",
                "critical" => "Ты — строгий преподаватель-методист. Проведи критический анализ формулировок этих вопросов на предмет двусмысленности.",
                "report" => "Ты — AI-ассистент. Составь развернутый структурированный отчет и дай рекомендации для преподавателя по изменению учебного плана.",
                _ => "Проанализируй ошибки в вопросах и дай краткие рекомендации."
            };

            return await SendApiRequestAsync(systemPrompt, sb.ToString(), modelProvider.ToLower());
        }

        private async Task<string> SendApiRequestAsync(string systemPrompt, string userContent, string modelProvider)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AIAnalyzerApp/1.0");

            string apiUrl = "";
            string apiKey = "";
            string modelName = "";

            if (modelProvider == "gigachat")
            {
                apiUrl = _configuration["AiSettings:GigaChat:ApiUrl"];
                apiKey = _configuration["AiSettings:GigaChat:ApiKey"];
                modelName = _configuration["AiSettings:GigaChat:ModelName"] ?? "GigaChat:latest";
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
            else
            {
                apiUrl = _configuration["AiSettings:DeepSeek:ApiUrl"];
                apiKey = _configuration["AiSettings:DeepSeek:ApiKey"];
                modelName = _configuration["AiSettings:DeepSeek:ModelName"] ?? "deepseek-chat";
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                },
                temperature = 0.7
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    var errorLog = await response.Content.ReadAsStringAsync();
                    return $"Ошибка {modelProvider} API ({response.StatusCode}): {errorLog}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "Нейросеть вернула пустой answer.";
                }

                return "Не удалось распарсить ответ от ИИ.";
            }
            catch (Exception ex)
            {
                return $"Ошибка при обращении к {modelProvider}: {ex.Message}";
            }
        }
    }
}