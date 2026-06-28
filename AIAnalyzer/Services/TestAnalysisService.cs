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

            var questionStats = new Dictionary<string, QuestionStatDto>();

            int metadataOffset = 4;
            int columnStride = 4;
            var processedStudents = new HashSet<string>();

            // ШАГ 1: Извлекаем все "таблицы". 
            // 1 CSV = 1 таблица. 1 многостраничный XLSX = Несколько таблиц.
            var allTables = new List<List<string[]>>();

            foreach (var file in answersData)
            {
                allTables.AddRange(ExtractTables(file.stream, file.fileName));
            }

            // ШАГ 2: Обрабатываем каждую таблицу (каждый лист) независимо
            foreach (var rows in allTables)
            {
                if (rows.Count < 2) continue; // Пропускаем пустые листы

                var headers = rows[0];
                var currentFileQuestions = new Dictionary<int, string>();

                for (int i = metadataOffset; i < headers.Length; i += columnStride)
                {
                    var qText = headers[i].Replace("\"", "").Trim();
                    if (!string.IsNullOrWhiteSpace(qText))
                    {
                        currentFileQuestions[i] = qText;

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

                // Обработка ответов (начинаем со 2-й строки текущего листа)
                for (int r = 1; r < rows.Count; r++)
                {
                    var row = rows[r];
                    if (row.Length <= metadataOffset) continue;

                    var userId = row[0].Trim();
                    if (string.IsNullOrWhiteSpace(userId) || userId.Contains("Среднее")) continue;

                    bool studentHasAnswers = false;

                    foreach (var kvp in currentFileQuestions)
                    {
                        int colIndex = kvp.Key;
                        string qText = kvp.Value;

                        if (colIndex + 3 >= row.Length) continue;

                        string qType = row[colIndex].Trim();
                        string studentAnswer = row[colIndex + 2].Replace("\"", "").Trim().ToLowerInvariant();
                        string correctAnswer = row[colIndex + 3].Replace("\"", "").Trim().ToLowerInvariant();

                        if (string.IsNullOrWhiteSpace(qType) && string.IsNullOrWhiteSpace(studentAnswer) && string.IsNullOrWhiteSpace(correctAnswer))
                            continue;

                        studentHasAnswers = true;

                        if (studentAnswer == correctAnswer)
                        {
                            questionStats[qText].CorrectCount++;
                        }
                        else
                        {
                            questionStats[qText].ErrorsCount++;
                        }
                    }

                    if (studentHasAnswers)
                    {
                        string attemptId = userId + "_" + (row.Length > 1 ? row[1].Trim() : "");
                        processedStudents.Add(attemptId);
                    }
                }
            }

            // ОСТАЛЬНАЯ ЧАСТЬ КОДА БЕЗ ИЗМЕНЕНИЙ (Расчет процентов и зон)
            result.TotalStudentsAnalyzed = processedStudents.Count;
            double totalErrorPercentage = 0;
            int validQuestionsCount = 0;

            foreach (var stat in questionStats.Values)
            {
                if (stat.TotalAttempts > 0)
                {
                    stat.Zone = stat.ErrorPercentage switch
                    {
                        >= 60 => ErrorZone.Red,
                        >= 40 => ErrorZone.Yellow,
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

            result.Questions = result.Questions.OrderByDescending(q => q.ErrorPercentage).ToList();

            return result;
        }

        /// <summary>
        /// Извлекает листы из Excel или разбирает CSV в список таблиц.
        /// Возвращает коллекцию таблиц (где таблица — это список строк массива).
        /// </summary>
        private IEnumerable<List<string[]>> ExtractTables(Stream stream, string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension == ".xlsx" || extension == ".xls")
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                // Читаем Excel, пока не закончатся листы (вкладки)
                do
                {
                    var currentSheetRows = new List<string[]>();
                    while (reader.Read())
                    {
                        var row = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                        }
                        currentSheetRows.Add(row);
                    }

                    if (currentSheetRows.Count > 0)
                    {
                        yield return currentSheetRows; // Возвращаем собранный лист
                    }
                }
                while (reader.NextResult()); // Переходим к следующему листу, если он есть
            }
            else
            {
                // Для CSV это всегда одна таблица
                var csvRows = new List<string[]>();
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    csvRows.Add(ParseCsvLine(line));
                }

                if (csvRows.Count > 0)
                {
                    yield return csvRows;
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