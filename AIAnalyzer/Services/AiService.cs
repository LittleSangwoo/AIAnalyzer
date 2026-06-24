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
    public interface IAiService
    {
        Task<string> GenerateRecommendationAsync(List<QuestionStatDto> redZoneQuestions, string promptType, string modelProvider);
    }

    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> GenerateRecommendationAsync(List<QuestionStatDto> redZoneQuestions, string promptType, string modelProvider)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Статистика проблемных вопросов (Красная зона — высокий процент ошибок):");
            foreach (var q in redZoneQuestions)
            {
                sb.AppendLine($"- Вопрос: \"{q.QuestionText}\" | Процент ошибок: {q.ErrorPercentage}% ({q.ErrorsCount} из {q.TotalAttempts} попыток)");
            }

            // Умная логика промптов
            string systemPrompt = promptType.ToLower() switch
            {
                "rewrite" => "Ты — эксперт-тестолог. Студенты массово ошибаются в этих вопросах. Предложи 2-3 варианта переформулировки для каждого вопроса, чтобы убрать двусмысленность, но сохранить проверку знаний. Объясни, почему текущая формулировка может быть сложной.",

                "course_redesign" => "Ты — опытный преподаватель-методист. Проанализируй темы вопросов с высоким процентом ошибок. Дай рекомендации: какие темы курса нужно объяснить глубже? Какие дополнительные материалы или упражнения помогут студентам лучше усвоить этот материал?",

                "test_redesign" => "Ты — специалист по оценке знаний. Посмотри на провальные вопросы. Дай рекомендации, как изменить структуру самого теста: стоит ли поменять формат вопросов (например, добавить открытые вопросы), изменить веса баллов или добавить интерактивные подсказки?",

                _ => "Ты — аналитик учебных данных. Проанализируй ошибки и дай рекомендации по улучшению учебного процесса."
            };

            return await SendApiRequestAsync(systemPrompt, sb.ToString(), modelProvider.ToLower());
        }

        private async Task<string> SendApiRequestAsync(string systemPrompt, string userContent, string modelProvider)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AIAnalyzerApp/1.0");

            // Получаем настройки из appsettings.json
            string section = modelProvider == "gigachat" ? "GigaChat" : "DeepSeek";
            string apiUrl = _configuration[$"AiSettings:{section}:ApiUrl"];
            string apiKey = _configuration[$"AiSettings:{section}:ApiKey"];
            string modelName = _configuration[$"AiSettings:{section}:ModelName"] ?? "default-model";

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

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
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"Ошибка {modelProvider}: {responseString}";

                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            catch (Exception ex)
            {
                return $"Ошибка связи с {modelProvider}: {ex.Message}";
            }
        }
    }
}