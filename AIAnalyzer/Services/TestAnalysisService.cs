using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;
using AIAnalyzer.ViewModels;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AIAnalyzer.Services
{
    public class TestAnalysisService : ITestAnalysisService
    {
        public AnalysisResultViewModel Analyze(List<(Stream stream, string fileName)> answersData)
        {
            var result = new AnalysisResultViewModel();
            result.TotalFilesAnalyzed = answersData.Count;

            // Словарь для хранения статистики по вопросам. Ключ — текст вопроса, чтобы объединять 
            // одинаковые вопросы из разных файлов.
            var questionStats = new Dictionary<string, QuestionStatDto>();

            int metadataOffset = 4; // Первые 4 колонки — Пользователь, Дата, Статус, Баллы
            int columnStride = 4;   // Шаг: Тип вопроса, Хэш, Ответ студента, Правильный ответ

            // HashSet для подсчета уникальных попыток студентов (ID + Дата)
            var processedStudents = new HashSet<string>();

            foreach (var file in answersData)
            {
                var rows = ReadRows(file.stream, file.fileName).ToList();
                if (rows.Count < 2) continue; // Пропускаем пустые файлы или файлы только с заголовком

                var headers = rows[0];

                // Карта вопросов для текущего файла (Индекс колонки -> Текст вопроса)
                var currentFileQuestions = new Dictionary<int, string>();

                for (int i = metadataOffset; i < headers.Length; i += columnStride)
                {
                    var qText = headers[i].Replace("\"", "").Trim();
                    if (!string.IsNullOrWhiteSpace(qText))
                    {
                        currentFileQuestions[i] = qText;

                        // Если такой вопрос встречается впервые, добавляем его в общую статистику
                        if (!questionStats.ContainsKey(qText))
                        {
                            questionStats[qText] = new QuestionStatDto
                            {
                                QuestionId = $"Q{questionStats.Count + 1}",
                                QuestionText = qText,
                                CorrectCount = 0,
                                ErrorsCount = 0
                            };
                        }
                    }
                }

                // Обработка ответов студентов (начинаем со 2-й строки)
                for (int r = 1; r < rows.Count; r++)
                {
                    var row = rows[r];
                    if (row.Length <= metadataOffset) continue;

                    var userId = row[0].Trim();

                    // Если строка пустая или служебная (например, "Среднее"), пропускаем
                    if (string.IsNullOrWhiteSpace(userId) || userId.Contains("Среднее")) continue;

                    bool studentHasAnswers = false;

                    foreach (var kvp in currentFileQuestions)
                    {
                        int colIndex = kvp.Key;
                        string qText = kvp.Value;

                        // Защита от выхода за пределы массива
                        if (colIndex + 3 >= row.Length) continue;

                        string qType = row[colIndex].Trim();
                        string studentAnswer = row[colIndex + 2].Replace("\"", "").Trim().ToLowerInvariant();
                        string correctAnswer = row[colIndex + 3].Replace("\"", "").Trim().ToLowerInvariant();

                        // Если пусто, значит вопрос из банка не выпадал этому студенту в этой попытке
                        if (string.IsNullOrWhiteSpace(qType) && string.IsNullOrWhiteSpace(studentAnswer) && string.IsNullOrWhiteSpace(correctAnswer))
                            continue;

                        studentHasAnswers = true;

                        // Сравниваем ответ с эталоном из этой же строки
                        if (studentAnswer == correctAnswer)
                        {
                            questionStats[qText].CorrectCount++;
                        }
                        else
                        {
                            questionStats[qText].ErrorsCount++;
                        }
                    }

                    // Если студент ответил хотя бы на один вопрос, засчитываем попытку
                    if (studentHasAnswers)
                    {
                        string attemptId = userId + "_" + (row.Length > 1 ? row[1].Trim() : "");
                        processedStudents.Add(attemptId);
                    }
                }
            }

            result.TotalStudentsAnalyzed = processedStudents.Count;
            double totalErrorPercentage = 0;
            int validQuestionsCount = 0;

            foreach (var stat in questionStats.Values)
            {
                // Защита от деления на ноль, если на вопрос вообще никто не отвечал
                if (stat.TotalAttempts > 0)
                {
                    // Распределяем по зонам на основе ПРОЦЕНТА ошибок
                    stat.Zone = stat.ErrorPercentage switch
                    {
                        >= 50 => ErrorZone.Red,
                        >= 30 => ErrorZone.Yellow,
                        _ => ErrorZone.Green
                    };

                    totalErrorPercentage += stat.ErrorPercentage;
                    validQuestionsCount++;
                    result.Questions.Add(stat);
                }
            }

            result.AverageErrorPercentage = validQuestionsCount > 0
                ? Math.Round(totalErrorPercentage / validQuestionsCount, 2)
                : 0;

            // Сортируем: сначала самые проблемные (где процент ошибок выше)
            result.Questions = result.Questions.OrderByDescending(q => q.ErrorPercentage).ToList();

            return result;
        }

        /// <summary>
        /// Универсальный метод чтения строк (поддерживает и CSV, и XLSX)
        /// </summary>
        private IEnumerable<string[]> ReadRows(Stream stream, string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension == ".xlsx" || extension == ".xls")
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                using var reader = ExcelReaderFactory.CreateReader(stream);
                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                    }
                    yield return row;
                }
            }
            else
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return ParseCsvLine(line);
                }
            }
        }

        private string[] ParseCsvLine(string? line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            var result = new List<string>();
            bool inQuotes = false;
            var currentBuilder = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentBuilder.ToString());
                    currentBuilder.Clear();
                }
                else currentBuilder.Append(c);
            }
            result.Add(currentBuilder.ToString());
            return result.ToArray();
        }
    }
}