using ClosedXML.Excel;
using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Models.Enums;
using System.IO;
using System.Collections.Generic;

namespace AIAnalyzer.Services
{
    // Этот интерфейс ищет AiController
    public interface IReportService
    {
        byte[] GenerateExcelReport(List<QuestionStatDto> questions, string aiRecommendation);
    }

    public class ReportService : IReportService
    {
        public byte[] GenerateExcelReport(List<QuestionStatDto> questions, string aiRecommendation)
        {
            using var workbook = new XLWorkbook();

            // ЛИСТ 1: СТАТИСТИКА ВОПРОСОВ
            var wsStats = workbook.Worksheets.Add("Статистика ошибок");

            var mainHeaderCell = wsStats.Cell("A1");
            mainHeaderCell.Value = "Анализ результатов тестирования (Светофор)";
            mainHeaderCell.Style.Font.SetBold(true);               
            mainHeaderCell.Style.Font.SetFontSize(16);             
            mainHeaderCell.Style.Font.SetFontColor(XLColor.FromHtml("#1F497D")); 

            string[] headers = { "ID Вопроса", "Текст вопроса", "Правильные ответы", "Количество ошибок", "Зона опасности" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = wsStats.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true);                     
                cell.Style.Font.SetFontColor(XLColor.White);       
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F3542");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int currentRow = 4;
            foreach (var q in questions)
            {
                wsStats.Cell(currentRow, 1).Value = q.QuestionId;
                wsStats.Cell(currentRow, 2).Value = q.QuestionText;
                wsStats.Cell(currentRow, 3).Value = q.CorrectCount;
                wsStats.Cell(currentRow, 4).Value = q.ErrorsCount;

                var zoneCell = wsStats.Cell(currentRow, 5);
                zoneCell.Value = q.Zone.ToString();
                zoneCell.Style.Font.SetBold(true);                 
                zoneCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (q.Zone == ErrorZone.Red || q.ErrorsCount >= 6)
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD2D2");
                    zoneCell.Style.Font.SetFontColor(XLColor.DarkRed); 
                }
                else if (q.Zone == ErrorZone.Yellow || (q.ErrorsCount >= 4 && q.ErrorsCount < 6))
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEAA7");
                    zoneCell.Style.Font.SetFontColor(XLColor.FromHtml("#D35400")); 
                }
                else
                {
                    zoneCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4EDDA");
                    zoneCell.Style.Font.SetFontColor(XLColor.DarkGreen); 
                }

                for (int i = 1; i <= headers.Length; i++)
                {
                    wsStats.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                currentRow++;
            }

            wsStats.Columns().AdjustToContents();
            wsStats.Column(2).Width = 50;
            wsStats.Column(2).Style.Alignment.WrapText = true;

            // ЛИСТ 2: РЕКОМЕНДАЦИИ ИИ
            var wsAi = workbook.Worksheets.Add("Аналитика и рекомендации ИИ");

            var aiHeaderCell = wsAi.Cell("A1");
            aiHeaderCell.Value = "Заключение искусственного интеллекта";
            aiHeaderCell.Style.Font.SetBold(true);                 
            aiHeaderCell.Style.Font.SetFontSize(14);               
            aiHeaderCell.Style.Font.SetFontColor(XLColor.FromHtml("#1F497D")); 

            var reportRange = wsAi.Range("A3:H25");
            var mainCell = reportRange.FirstCell();

            mainCell.Value = string.IsNullOrWhiteSpace(aiRecommendation)
                ? "Внимание: Анализ ИИ для данного отчета не запускался."
                : aiRecommendation;

            mainCell.Style.Alignment.WrapText = true;
            mainCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            mainCell.Style.Font.SetFontSize(11);                   

            reportRange.Merge();
            reportRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            reportRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}