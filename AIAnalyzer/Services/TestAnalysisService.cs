using AIAnalyzer.Models.Enums;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.ViewModels;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AIAnalyzer.Services
{

    public class TestAnalysisService : ITestAnalysisService
    {
        public AnalysisResultViewModel Analyze(Stream etalonStream, Stream answersStream)
        {
            var result = new AnalysisResultViewModel();

            // 1. Читаем эталон (упрощенно: первая строка - заголовки/номера, вторая - правильные ответы)
            using var etalonReader = new StreamReader(etalonStream);
            var headers = etalonReader.ReadLine()?.Split(',');
            var correctAnswers = etalonReader.ReadLine()?.Split(',');

            // 2. Читаем ответы пользователей
            using var answersReader = new StreamReader(answersStream);
            var userAnswersHeaders = answersReader.ReadLine(); // Пропускаем заголовки

            var questionStats = new Dictionary<int, QuestionStatDto>();
            // Инициализируем статистику (начинаем с колонок, где идут ответы, допустим индекс 4)
            int answerStartIndex = 4;
            for (int i = answerStartIndex; i < headers.Length; i++)
            {
                questionStats[i] = new QuestionStatDto
                {
                    QuestionId = headers[i],
                    QuestionText = headers[i], // Сюда можно подтягивать полный текст, если он есть
                    ErrorsCount = 0,
                    CorrectCount = 0
                };
            }

            string line;
            while ((line = answersReader.ReadLine()) != null)
            {
                var columns = line.Split(',');
                if (columns.Length < answerStartIndex) continue;

                var score = columns[2]; // Допустим, баллы в 3-й колонке (индекс 2)

                // Отбрасываем нулевые попытки
                if (string.IsNullOrWhiteSpace(score) || score.Trim().StartsWith("0/"))
                    continue;

                result.TotalStudentsAnalyzed++;

                // Сверяем с эталоном
                for (int i = answerStartIndex; i < columns.Length && i < correctAnswers.Length; i++)
                {
                    if (columns[i].Trim() == correctAnswers[i].Trim())
                    {
                        questionStats[i].CorrectCount++;
                    }
                    else
                    {
                        questionStats[i].ErrorsCount++;
                    }
                }
            }

            // 3. Распределяем по зонам "Светофора"
            foreach (var stat in questionStats.Values)
            {
                stat.Zone = stat.ErrorsCount switch
                {
                    >= 6 => ErrorZone.Red,
                    >= 4 => ErrorZone.Yellow,
                    > 0 => ErrorZone.Green,
                    _ => ErrorZone.Green
                };
                result.Questions.Add(stat);
            }

            // Сортируем: сначала проблемные (красные)
            result.Questions = result.Questions.OrderByDescending(q => (int)q.Zone).ToList();

            return result;
        }
    }
}
