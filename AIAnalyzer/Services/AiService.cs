using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using AIAnalyzer.Models.DTOs;
using System.Net.Http.Headers;

namespace AIAnalyzer.Services
{
    public interface IAiService
    {
        Task<string> GenerateRecommendationAsync(List<QuestionStatDto> targetQuestions, string promptType, string modelProvider, string userApiKey);
        Task<string> ProcessCustomPromptAsync(string userPrompt, string modelProvider, string userApiKey);
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

        private async Task<string> GetSberAccessTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            // Сбер требует уникальный UUID для каждого запроса на авторизацию
            client.DefaultRequestHeaders.Add("RqUID", Guid.NewGuid().ToString());

            // Берем ключ авторизации из конфига
            string authKey = _configuration["AiSettings:GigaChat:AuthorizationKey"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authKey);

            var content = new FormUrlEncodedContent(new[]
             {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });

            string authUrl = _configuration["AiSettings:GigaChat:AuthUrl"];
            var response = await client.PostAsync(authUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Не удалось получить токен Сбера: {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<string> GenerateRecommendationAsync(List<QuestionStatDto> targetQuestions, string promptType, string modelProvider, string userApiKey)
        {
            if (targetQuestions.Count <= 40 || promptType.ToLower() == "rewrite")
            {
                return await ProcessSingleBatchAsync(targetQuestions, promptType, modelProvider, userApiKey);
            }

            var chunks = targetQuestions.Chunk(40).ToList();
            var intermediateAnalyses = new List<string>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var sb = new StringBuilder();
                sb.AppendLine($"ЧАСТЬ ДАННЫХ {i + 1} ИЗ {chunks.Count}:");

                foreach (var q in chunk)
                {
                    sb.AppendLine($"- [{q.QuestionId}] \"{q.QuestionText}\" | Ошибок: {q.ErrorPercentage}% ({q.ErrorsCount} нев., {q.CorrectCount} вер.)");

                    // Передаем ответы студентов для анализа, если они есть
                    if (q.SampleAnswers != null && q.SampleAnswers.Any())
                    {
                        sb.AppendLine($"  Ответы студентов: {string.Join(", ", q.SampleAnswers.Take(10))}");
                    }
                }

                string intermediatePrompt = "Ты — аналитик. Твоя задача кратко выписать основные паттерны ошибок и проблемные темы из этой части данных. Не пиши длинных вступлений, только суть. Это пойдет в финальный отчет.";

                string aiResponse = await SendApiRequestAsync(intermediatePrompt, sb.ToString(), modelProvider.ToLower(), userApiKey);
                intermediateAnalyses.Add($"### Анализ части {i + 1}\n{aiResponse}");

                if (i < chunks.Count - 1)
                {
                    await Task.Delay(15000);
                }
            }

            var finalSb = new StringBuilder();
            finalSb.AppendLine("Вот промежуточные выводы по всем частям курса:");
            finalSb.AppendLine();
            finalSb.AppendLine(string.Join("\n\n", intermediateAnalyses));

            string finalSystemPrompt = promptType.ToLower() switch
            {
                "course_redesign" => @"Ты — старший методист образовательных программ. Тебе переданы скомпилированные отчеты об ошибках по всему курсу.
Сделай глобальный вывод:
1. Какие темы или концепции студенты массово не понимают?
2. Дай 3-5 стратегических рекомендаций по доработке курса.
Отвечай структурировано, в Markdown (заголовки ##, списки). Не упоминай, что данные были разбиты на части.",

                "test_redesign" => @"Ты — специалист по оценке знаний. Перед тобой собранные выводы по провалам в тесте.
Предложи, как изменить формат тестирования:
1. Какие паттерны ошибок вид во всем тесте?
2. Как сбалансировать тест и типы вопросов?
Отвечай структурировано, в Markdown.",

                _ => "Сделай финальный вывод на основе переданных данных. Используй Markdown."
            };

            await Task.Delay(5000);

            return await SendApiRequestAsync(finalSystemPrompt, finalSb.ToString(), modelProvider.ToLower(), userApiKey);
        }

        private async Task<string> ProcessSingleBatchAsync(IEnumerable<QuestionStatDto> targetQuestions, string promptType, string modelProvider, string userApiKey)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ДАННЫЕ:");
            foreach (var q in targetQuestions)
            {
                sb.AppendLine($"- [{q.QuestionId}] \"{q.QuestionText}\" | Ошибок: {q.ErrorPercentage}% ({q.ErrorsCount} нев., {q.CorrectCount} вер.)");

                // Передаем ответы студентов
                if (q.SampleAnswers != null && q.SampleAnswers.Any())
                {
                    sb.AppendLine($"  Ответы студентов: {string.Join(", ", q.SampleAnswers.Take(10))}");
                }
            }

            string systemPrompt = promptType.ToLower() switch
            {
                "rewrite" => @"Ты — строгий методист и лингвист-тестолог. Твоя задача — не просто исправить вопросы, а найти ПРИЧИНУ массовых провалов.
                Для КАЖДОГО вопроса:
                1. Анализ провала: Сформулируй гипотезу. ВНИМАТЕЛЬНО проверь 'Ответы студентов' (если они переданы). Если студенты дают верный по смыслу ответ, но СДО его не засчитала (из-за регистра, опечатки, пробела или синонима), прямо укажи: 'Ошибка верификации СДО. Ответы студентов по сути верны'. Если таких ответов нет, укажи другую причину (сложная грамматическая конструкция, неясная формулировка).
                2. Вердикт: Обязательно ли менять вопрос? (Например: 'Вопрос критически неисправен' или 'Требует перенастройки ответов в системе').
                3. Исправление: Напиши идеальную, академически грамотную формулировку, которая исключает двоякое толкование и даст понимание в каком именно формате должен быть предоставлен ответ'.

                Отвечай в формате таблицы или четкого списка с заголовками. Твоя цель — сделать тест проверяющим знания, а не умение разгадывать ребусы или угадывать регистр.",

                "course_redesign" => @"Ты — строгий методист. Посмотри на картину проваленных вопросов:
                1. Какие темы студенты массово не понимают?
                2. Где не хватает материалов или практики?
                3. Дай 3-5 стратегических рекомендаций по курсу.
                Отвечай структурировано, в Markdown.",

                "test_redesign" => @"Ты — специалист по оценке знаний. Перед тобой статистика провалов.
                Предложи, как изменить формат тестирования:
                1. Стоит ли изменить тип проблемных вопросов?
                2. Какие паттерны ошибок видны?
                3. Как сбалансировать тест?
                Отвечай структурировано, в Markdown.",

                _ => "Проанализируй статистику. Используй Markdown."
            };

            return await SendApiRequestAsync(systemPrompt, sb.ToString(), modelProvider.ToLower(), userApiKey);
        }

        public async Task<string> ProcessCustomPromptAsync(string userPrompt, string modelProvider, string userApiKey)
        {
            string systemPrompt = "Ты — умный помощник преподавателя и аналитик учебных курсов. Отвечай на вопросы пользователя профессионально, четко, по делу и с примерами. Обязательно используй Markdown (заголовки, списки, жирный текст) для удобства чтения.";
            return await SendApiRequestAsync(systemPrompt, userPrompt, modelProvider.ToLower(), userApiKey);
        }

        private async Task<string> SendApiRequestAsync(string systemPrompt, string userContent, string modelProvider, string userApiKey)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AIAnalyzerApp/1.0");

            string section = modelProvider switch
            {
                "gigachat" => "GigaChat",
                "groq" => "Groq",
                "local" => "LocalAI", // Направляем на новую секцию в appsettings.json
                _ => throw new InvalidOperationException($"Провайдер '{modelProvider}' не настроен в системе.")
            };

            string apiUrl = _configuration[$"AiSettings:{section}:ApiUrl"];
            string modelName = _configuration[$"AiSettings:{section}:ModelName"];

            // 1. Берем ключ, который ввели на сайте
            string finalApiKey = userApiKey;

            // 2. Если на сайте пусто, решаем, откуда брать ключ
            if (string.IsNullOrWhiteSpace(finalApiKey))
            {
                if (modelProvider.ToLower() == "gigachat")
                {
                    // Идем за свежим токеном в Сбер
                    finalApiKey = await GetSberAccessTokenAsync();
                }
                else
                {
                    // Берем постоянный ключ из appsettings
                    finalApiKey = _configuration[$"AiSettings:{section}:ApiKey"];
                }
            }

            if (string.IsNullOrWhiteSpace(finalApiKey))
                throw new InvalidOperationException($"Ключ API для '{section}' не передан в интерфейсе и не найден в настройках.");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", finalApiKey.Trim());

            // Добавляем принудительное требование русского языка к любому системному промпту
            string forcedSystemPrompt = $"{systemPrompt} Твой язык общения — русский. Все ответы, рекомендации и заголовки должны быть на русском языке.";

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = forcedSystemPrompt },
                    new { role = "user", content = userContent }
                },
                temperature = 0.5
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ошибка API ({response.StatusCode}): {responseString}");

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                return choices[0].GetProperty("message").GetProperty("content").GetString();
            }

            throw new InvalidOperationException("Ответ получен, но формат JSON не распознан.");
        }
    }
}