using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;

namespace AIAnalyzer.Services
{
    public interface IReportService
    {
        byte[] GenerateExcelReport(List<QuestionStatDto> questions, List<string> aiRecommendations);
    }

    public class ReportService : IReportService
    {
        public byte[] GenerateExcelReport(List<QuestionStatDto> questions, List<string> aiRecommendations)
        {
            using var workbook = new XLWorkbook();

            // ====================================================================
            // ЛИСТ 1: ТОЛЬКО СТАТИСТИКА
            // ====================================================================
            var wsStats = workbook.Worksheets.Add("Статистика ошибок");

            var mainHeaderCell = wsStats.Cell("A1");
            mainHeaderCell.Value = "Анализ результатов тестирования (Светофор)";
            mainHeaderCell.Style.Font.Bold = true;
            mainHeaderCell.Style.Font.FontSize = 16;
            mainHeaderCell.Style.Font.FontColor = XLColor.FromHtml("#1F497D");

            string[] headersStats = { "ID Вопроса", "Текст вопроса", "Правильные ответы", "Количество ошибок", "Процент ошибок", "Зона опасности" };
            for (int i = 0; i < headersStats.Length; i++)
            {
                var cell = wsStats.Cell(3, i + 1);
                cell.Value = headersStats[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F3542");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int statRow = 4;
            foreach (var q in questions)
            {
                wsStats.Cell(statRow, 1).Value = q.QuestionId;
                wsStats.Cell(statRow, 2).Value = q.QuestionText;
                wsStats.Cell(statRow, 3).Value = q.CorrectCount;
                wsStats.Cell(statRow, 4).Value = q.ErrorsCount;

                wsStats.Cell(statRow, 5).Value = q.ErrorPercentage + "%";
                wsStats.Cell(statRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var zoneCell = wsStats.Cell(statRow, 6);
                zoneCell.Value = q.Zone.ToString();
                zoneCell.Style.Font.Bold = true;
                zoneCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (q.Zone == ErrorZone.Red)
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD2D2");
                    zoneCell.Style.Font.FontColor = XLColor.DarkRed;
                }
                else if (q.Zone == ErrorZone.Yellow)
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEAA7");
                    zoneCell.Style.Font.FontColor = XLColor.FromHtml("#D35400");
                }
                else
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4EDDA");
                    zoneCell.Style.Font.FontColor = XLColor.DarkGreen;
                }

                for (int i = 1; i <= headersStats.Length; i++)
                {
                    wsStats.Cell(statRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    wsStats.Cell(statRow, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                }
                statRow++;
            }

            wsStats.Columns().AdjustToContents();
            wsStats.Column(2).Width = 80;
            wsStats.Column(2).Style.Alignment.WrapText = true;


            // ====================================================================
            // ЛИСТ 2: ИСПРАВЛЕНИЯ (ДО / ПОСЛЕ)
            // ====================================================================
            var wsCorrections = workbook.Worksheets.Add("Исправления ИИ");
            var corrHeaderCell = wsCorrections.Cell("A1");
            corrHeaderCell.Value = "Рекомендации ИИ по конкретным вопросам";
            corrHeaderCell.Style.Font.Bold = true;
            corrHeaderCell.Style.Font.FontSize = 14;
            corrHeaderCell.Style.Font.FontColor = XLColor.FromHtml("#1F497D");

            string[] headersCorr = { "ID Вопроса", "Исходный вопрос", "Исправление / Анализ ИИ" };
            for (int i = 0; i < headersCorr.Length; i++)
            {
                var cell = wsCorrections.Cell(3, i + 1);
                cell.Value = headersCorr[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F3542");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Парсинг ответов ИИ
            var specificRecs = new Dictionary<string, List<string>>();

            if (aiRecommendations != null)
            {
                foreach (var fullRec in aiRecommendations)
                {
                    if (fullRec.Contains("500") || fullRec.Contains("Error")) continue;

                    var paragraphs = fullRec.Split(new[] { "\n\n", "\r\n\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    string currentQuestionId = null;

                    foreach (var p in paragraphs)
                    {
                        bool foundId = false;
                        foreach (var q in questions)
                        {
                            string idStr = q.QuestionId.ToString();
                            if (Regex.IsMatch(p, $@"\b{Regex.Escape(idStr)}\b", RegexOptions.IgnoreCase))
                            {
                                currentQuestionId = idStr;
                                foundId = true;
                                break;
                            }
                        }

                        if (foundId)
                        {
                            if (!specificRecs.ContainsKey(currentQuestionId))
                                specificRecs[currentQuestionId] = new List<string>();
                            specificRecs[currentQuestionId].Add(p.Trim());
                        }
                        else if (currentQuestionId != null)
                        {
                            specificRecs[currentQuestionId].Add(p.Trim());
                        }
                    }
                }
            }

            int corrRow = 4;
            foreach (var kvp in specificRecs)
            {
                var originalQuestion = questions.FirstOrDefault(q => q.QuestionId.ToString() == kvp.Key);

                wsCorrections.Cell(corrRow, 1).Value = kvp.Key;
                wsCorrections.Cell(corrRow, 2).Value = originalQuestion?.QuestionText ?? "Текст вопроса не найден";
                wsCorrections.Cell(corrRow, 3).Value = string.Join("\n\n", kvp.Value);

                for (int i = 1; i <= headersCorr.Length; i++)
                {
                    wsCorrections.Cell(corrRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    wsCorrections.Cell(corrRow, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                }
                corrRow++;
            }

            if (specificRecs.Count == 0)
            {
                wsCorrections.Cell(4, 1).Value = "Конкретных привязок к ID вопросов не найдено.";
                wsCorrections.Range("A4:C4").Merge();
            }

            wsCorrections.Column(1).Width = 15;
            wsCorrections.Column(2).Width = 50;
            wsCorrections.Column(3).Width = 70;
            wsCorrections.Style.Alignment.WrapText = true;


            // 
            // ЛИСТ 3: ПОЛНЫЙ ВЫВОД ИИ
            var wsAiLog = workbook.Worksheets.Add("Полный лог ИИ");
            var aiLogHeader = wsAiLog.Cell("A1");
            aiLogHeader.Value = "Сырой вывод искусственного интеллекта (Все ответы)";
            aiLogHeader.Style.Font.Bold = true;
            aiLogHeader.Style.Font.FontSize = 14;
            aiLogHeader.Style.Font.FontColor = XLColor.FromHtml("#1F497D");

            int aiRow = 3;
            if (aiRecommendations == null || !aiRecommendations.Any())
            {
                wsAiLog.Cell(aiRow, 1).Value = "Анализ ИИ для данного отчета не запускался.";
            }
            else
            {
                foreach (var rec in aiRecommendations)
                {
                    // ФИЛЬТРАЦИЯ: убираем всё, что похоже на ошибку
                    if (rec.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
    rec.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
    rec.Contains("Bad Request", StringComparison.OrdinalIgnoreCase) ||
    rec.Contains("500", StringComparison.OrdinalIgnoreCase) ||
    rec.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var cell = wsAiLog.Cell(aiRow, 1);
                    cell.Value = rec;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.BottomBorderColor = XLColor.LightGray;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                    aiRow += 2;
                }
            }

            wsAiLog.Column(1).Width = 120;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}