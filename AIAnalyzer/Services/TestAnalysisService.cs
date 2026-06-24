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
        public AnalysisResultViewModel Analyze(Stream etalonStream, string etalonFileName, Stream answersStream, string answersFileName)
        {
            var result = new AnalysisResultViewModel();

            // 1. Читаем Эталон
            var etalonRows = ReadRows(etalonStream, etalonFileName).ToList();
            if (etalonRows.Count < 2) return result;

            var etalonHeaders = etalonRows[0];
            var correctAnswers = etalonRows[1];

            // Пропускаем служебную строку "Правильный ответ", если она есть
            if (correctAnswers.Length > 0 && correctAnswers[0].Contains("Правильный"))
            {
                correctAnswers = etalonRows.Count > 2 ? etalonRows[2] : Array.Empty<string>();
            }

            if (etalonHeaders.Length == 0 || correctAnswers.Length == 0) return result;

            // Инициализируем статистику
            var questionStats = new Dictionary<int, QuestionStatDto>();
            for (int i = 0; i < etalonHeaders.Length; i++)
            {
                var qText = etalonHeaders[i].Replace("\"", "").Trim();
                if (string.IsNullOrWhiteSpace(qText)) continue;

                questionStats[i] = new QuestionStatDto
                {
                    QuestionId = $"Q{i + 1}",
                    QuestionText = qText,
                    ErrorsCount = 0,
                    CorrectCount = 0
                };
            }

            // 2. Читаем Массив ответов (пропускаем первую строку с заголовками)
            var answersRows = ReadRows(answersStream, answersFileName).Skip(1);

            int studentAnswerOffset = 4; // Индекс первого ответа студента
            int columnStride = 4;        // Шаг между вопросами

            foreach (var columns in answersRows)
            {
                if (columns.Length <= studentAnswerOffset) continue;

                var score = columns[3]; // Баллы

                // Игнорируем нулевые попытки
                if (string.IsNullOrWhiteSpace(score) || score.Trim().StartsWith("0/") || score.Trim() == "0")
                    continue;

                result.TotalStudentsAnalyzed++;

                // Сверяем ответы с учетом шага колонок
                for (int etalonIndex = 0; etalonIndex < etalonHeaders.Length; etalonIndex++)
                {
                    int studentColIndex = studentAnswerOffset + (etalonIndex * columnStride);
                    if (studentColIndex >= columns.Length) break;

                    var studentAnswer = columns[studentColIndex].Replace("\"", "").Trim();
                    var correctAnswer = etalonIndex < correctAnswers.Length ? correctAnswers[etalonIndex].Replace("\"", "").Trim() : "";

                    if (string.Equals(studentAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        questionStats[etalonIndex].CorrectCount++;
                    }
                    else
                    {
                        questionStats[etalonIndex].ErrorsCount++;
                    }
                }
            }

            // 3. Формирование зон
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

        /// <summary>
        /// Универсальный метод чтения строк (поддерживает и CSV, и XLSX)
        /// </summary>
        private IEnumerable<string[]> ReadRows(Stream stream, string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension == ".xlsx" || extension == ".xls")
            {
                // Необходимая регистрация провайдера для ExcelDataReader
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                using var reader = ExcelReaderFactory.CreateReader(stream);
                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                    }
                    yield return row; // Ленивый возврат строки
                }
            }
            else
            {
                // Для CSV используем UTF-8 с BOM (стандарт выгрузок из Google Таблиц / нового Excel)
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return ParseCsvLine(line); // Ленивый возврат строки
                }
            }
        }

        // Оставляем твой старый парсер для CSV на случай, если загрузят текстовый файл
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