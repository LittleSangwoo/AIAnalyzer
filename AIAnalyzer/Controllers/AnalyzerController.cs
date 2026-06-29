using AIAnalyzer.Models.DTOs;
using AIAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzerController : ControllerBase
    {
        private readonly ITestAnalysisService _analysisService;
        private readonly IReportService _reportService;

        public AnalyzerController(ITestAnalysisService analysisService, IReportService reportService)
        {
            _analysisService = analysisService;
            _reportService = reportService;
        }

        public class ExportRequestDto
        {
            public List<QuestionStatDto> Questions { get; set; } = new();
            public List<string>? AiRecommendations { get; set; }
        }

        [HttpPost("export")]
        public IActionResult ExportToExcel([FromBody] ExportRequestDto request)
        {
            if (request.Questions == null || !request.Questions.Any())
                return BadRequest("Нет данных для выгрузки.");

            // Передаем список напрямую, если пуст — передаем пустой список
            var aiList = request.AiRecommendations ?? new List<string>();

            var fileBytes = _reportService.GenerateExcelReport(request.Questions, aiList);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SmartAnalytics_Report.xlsx");
        }

        [HttpPost("upload")]
        public IActionResult UploadFiles([FromForm] List<IFormFile> answersFiles)
        {
            if (answersFiles == null || answersFiles.Count == 0)
                return BadRequest("Необходимо загрузить хотя бы один файл с ответами.");

            try
            {
                var answersData = new List<(Stream stream, string fileName)>();
                foreach (var file in answersFiles)
                {
                    answersData.Add((file.OpenReadStream(), file.FileName));
                }

                var result = _analysisService.Analyze(answersData);

                foreach (var data in answersData)
                {
                    data.stream.Dispose();
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке файлов: {ex.Message}");
            }
        }
    }
}
