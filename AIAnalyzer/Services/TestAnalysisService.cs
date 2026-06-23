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

            // 1. ЖЕЛЕЗОБЕТОННАЯ РЕГИСТРАЦИЯ КОДИРОВОК (Прямо здесь!)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // 2. Теперь получаем windows-1251 без ошибок
            var encoding = System.Text.Encoding.GetEncoding("windows-1251");

            // Читаем эталон с нужной кодировкой
            using var etalonReader = new StreamReader(etalonStream, encoding);
            var etalonHeaders = ParseCsvLine(etalonReader.ReadLine());
            var correctAnswers = ParseCsvLine(etalonReader.ReadLine());

            if (correctAnswers != null && correctAnswers.Length > 0 && correctAnswers[0].Contains("Правильный"))
            {
                correctAnswers = ParseCsvLine(etalonReader.ReadLine());
            }

            if (etalonHeaders == null || correctAnswers == null) return result;

            // 2. Читаем ответы студентов с нужной кодировкой
            using var answersReader = new StreamReader(answersStream, encoding);
            answersReader.ReadLine(); // Пропуск заголовков

            var questionStats = new Dictionary<int, QuestionStatDto>();
            int studentAnswerOffset = 4; // Смещение: ответы начинаются с 5-го столбца

            for (int i = 0; i < etalonHeaders.Length; i++)
            {
                questionStats[i] = new QuestionStatDto
                {
                    QuestionId = $"Q{i + 1}",
                    QuestionText = etalonHeaders[i].Replace("\"", "").Trim(),
                    ErrorsCount = 0,
                    CorrectCount = 0
                };
            }

            string? line;
            while ((line = answersReader.ReadLine()) != null)
            {
                var columns = ParseCsvLine(line);
                if (columns.Length <= studentAnswerOffset) continue;

                var score = columns[3]; // Баллы

                // Фильтр пустых и нулевых попыток
                if (string.IsNullOrWhiteSpace(score) || score.Trim().StartsWith("0/") || score.Trim() == "0")
                    continue;

                result.TotalStudentsAnalyzed++;

                // Сверяем с эталоном
                for (int i = 0; i < etalonHeaders.Length; i++)
                {
                    int studentColIndex = i + studentAnswerOffset;
                    if (studentColIndex >= columns.Length) break;

                    var studentAnswer = columns[studentColIndex].Replace("\"", "").Trim();
                    var correctAnswer = i < correctAnswers.Length ? correctAnswers[i].Replace("\"", "").Trim() : "";

                    if (string.Equals(studentAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
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
