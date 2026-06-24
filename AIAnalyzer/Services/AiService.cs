using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using AIAnalyzer.Models.DTOs;
using System.Net.Http.Headers;

namespace AIAnalyzer.Services
{
    public interface IAiService
    {
        Task<string> GenerateRecommendationAsync(List<QuestionStatDto> redZoneQuestions, string promptType, string modelProvider);
        Task<string> ProcessCustomPromptAsync(string userPrompt, string modelProvider);
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
            sb.AppendLine("Статистика проблемных вопросов:");
            foreach (var q in redZoneQuestions)
            {
                sb.AppendLine($"- Вопрос: \"{q.QuestionText}\" | Ошибок: {q.ErrorsCount}, Верно: {q.CorrectCount}");
            }

            string systemPrompt = promptType.ToLower() switch
            {
                "rewrite" => "Ты — эксперт-тестолог. Переформулируй вопросы так, чтобы они стали ясными и профессиональными.",
                "course_redesign" => "Ты — методист. Проанализируй ошибки и дай рекомендации, как улучшить структуру курса.",
                "test_redesign" => "Ты — специалист по тестированию. Дай рекомендации по улучшению теста (варианты ответов, типы вопросов).",
                _ => "Ты — аналитик. Проанализируй данные и дай советы."
            };

            return await SendApiRequestAsync(systemPrompt, sb.ToString(), modelProvider.ToLower());
        }

        public async Task<string> ProcessCustomPromptAsync(string userPrompt, string modelProvider)
        {
            string systemPrompt = "Ты — помощник преподавателя и аналитик учебных курсов. Отвечай на вопросы пользователя профессионально, четко и с примерами.";
            return await SendApiRequestAsync(systemPrompt, userPrompt, modelProvider.ToLower());
        }

        private async Task<string> SendApiRequestAsync(string systemPrompt, string userContent, string modelProvider)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AIAnalyzerApp/1.0");

            // ИСПОЛЬЗУЕМ СТРОГИЙ ВЫБОР СЕКЦИИ
            string section = modelProvider.ToLower() switch
            {
                "gigachat" => "GigaChat",
                "groq" => "Groq",
                _ => throw new InvalidOperationException($"Провайдер '{modelProvider}' не настроен в системе.")
            };

            string apiUrl = _configuration[$"AiSettings:{section}:ApiUrl"];
            string apiKey = _configuration[$"AiSettings:{section}:ApiKey"];
            string modelName = _configuration[$"AiSettings:{section}:ModelName"];

            // Теперь, если ключа нет, мы получим понятную ошибку, а не NullReference
            if (string.IsNullOrEmpty(apiKey))
                return $"Ошибка: API Ключ для '{section}' пуст. Проверьте appsettings.json";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());

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

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"Ошибка API ({response.StatusCode}): {responseString}";

                using var doc = JsonDocument.Parse(responseString);

                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString();
                }

                return "Ответ получен, но формат JSON не содержит данных 'choices'.";
            }
            catch (Exception ex)
            {
                return $"Ошибка выполнения запроса: {ex.Message}";
            }
        }
        }
    }
    