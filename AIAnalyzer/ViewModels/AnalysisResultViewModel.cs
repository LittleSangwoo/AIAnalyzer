using AIAnalyzer.Models.DTOs;

namespace AIAnalyzer.ViewModels
{
    public class AnalysisResultViewModel
    {
        public int TotalStudentsAnalyzed { get; set; }
        public List<QuestionStatDto> Questions { get; set; } = new();
    }
}
