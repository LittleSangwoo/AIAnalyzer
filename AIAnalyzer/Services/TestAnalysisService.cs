using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;
using AIAnalyzer.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace AIAnalyzer.Services
{

    public class TestAnalysisService : ITestAnalysisService
    {
        public AnalysisResultViewModel Analyze(Stream etalonStream, Stream answersStream)
        {
            var result = new AnalysisResultViewModel();

            // 1. Читаем эталон
            using var etalonReader = new StreamReader(etalonStream);
            var headers = ParseCsvLine(etalonReader.ReadLine());
            var correctAnswers = ParseCsvLine(etalonReader.ReadLine());

            // Если во второй строке эталона служебное слово (например, "Правильный ответ"), читаем следующую
            if (correctAnswers != null && correctAnswers.Length > 0 && correctAnswers[0].Contains("Правильный"))
            {
                correctAnswers = ParseCsvLine(etalonReader.ReadLine());
            }

            if (headers == null || correctAnswers == null) return result;

            // 2. Читаем ответы студентов
            using var answersReader = new StreamReader(answersStream);
            answersReader.ReadLine(); // Пропускаем строку заголовков

            var questionStats = new Dictionary<int, QuestionStatDto>();

            // В выгрузках ответы обычно начинаются с 4-й колонки (индекс 4)
            int answerStartIndex = 4;
            for (int i = answerStartIndex; i < headers.Length; i++)
            {
                questionStats[i] = new QuestionStatDto
                {
                    QuestionId = $"Q{i - answerStartIndex + 1}",
                    QuestionText = headers[i].Replace("\"", "").Trim(),
                    ErrorsCount = 0,
                    CorrectCount = 0
                };
            }

            string? line;
            while ((line = answersReader.ReadLine()) != null)
            {
                var columns = ParseCsvLine(line);
                if (columns.Length < answerStartIndex) continue;

                // Индекс 3 - это колонка "Баллы" (например: "15 / 15 (100%)")
                var score = columns[3];

                // Исключаем нулевые результаты (отбрасываем тех, кто ничего не решил)
                if (string.IsNullOrWhiteSpace(score) || score.Trim().StartsWith("0/"))
                    continue;

                result.TotalStudentsAnalyzed++;

                // Сверяем ответы с эталоном
                for (int i = answerStartIndex; i < columns.Length && i < correctAnswers.Length; i++)
                {
                    if (!questionStats.ContainsKey(i)) continue;

                    var studentAnswer = columns[i].Trim();
                    var correctAnswer = correctAnswers[i].Trim();

                    // Строгое сравнение (в ТЗ сказано: СДО чувствительна к опечаткам)
                    if (studentAnswer == correctAnswer)
                    {
                        questionStats[i].CorrectCount++;
                    }
                    else
                    {
                        questionStats[i].ErrorsCount++;
                    }
                }
            }

            // 3. Алгоритм "Светофора"
            foreach (var stat in questionStats.Values)
            {
                stat.Zone = stat.ErrorsCount switch
                {
                    >= 6 => ErrorZone.Red,
                    >= 4 => ErrorZone.Yellow,
                    _ => ErrorZone.Green
                };
                result.Questions.Add(stat);
            }

            // Отсортировать: Красные вопросы в самом верху списка
            result.Questions = result.Questions.OrderByDescending(q => (int)q.Zone).ToList();

            return result;
        }

        // Вспомогательный метод для правильного парсинга CSV (игнорирует запятые внутри кавычек)
        private string[] ParseCsvLine(string? line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            var result = new List<string>();
            bool inQuotes = false;
            var currentBuilder = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes; // Переключаем флаг кавычек
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentBuilder.ToString());
                    currentBuilder.Clear();
                }
                else
                {
                    currentBuilder.Append(c);
                }
            }
            result.Add(currentBuilder.ToString());
            return result.ToArray();
        }
    }
}
