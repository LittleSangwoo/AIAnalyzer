using AIAnalyzer.Models.DTOs;
using System.Collections.Generic;

namespace AIAnalyzer.ViewModels
{
    public class AnalysisResultViewModel
    {
        public int TotalFilesAnalyzed { get; set; }
        public int TotalStudentsAnalyzed { get; set; }
        public double AverageErrorPercentage { get; set; }
        public List<QuestionStatDto> Questions { get; set; } = new();
    }
}