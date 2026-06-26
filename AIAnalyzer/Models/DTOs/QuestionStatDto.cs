using AIAnalyzer.Models.Enums;

namespace AIAnalyzer.Models.DTOs
{
    public class QuestionStatDto
    {
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public int CorrectCount { get; set; }
        public int ErrorsCount { get; set; }
        public ErrorZone Zone { get; set; }
<<<<<<< HEAD
        public List<string>? SampleAnswers { get; set; }
=======
        public string? CustomModelName { get; set; }
>>>>>>> 16c58496517c0b7e5c45f60cb278a929f31394a6

        // Оставляем поле для напарницы, куда её ИИ-сервис запишет ответ
        public string? AiRecommendation { get; set; }
        public int TotalAttempts => CorrectCount + ErrorsCount;
        public double ErrorPercentage => TotalAttempts == 0 ? 0 : Math.Round((double)ErrorsCount / TotalAttempts * 100, 2);
    }
}
