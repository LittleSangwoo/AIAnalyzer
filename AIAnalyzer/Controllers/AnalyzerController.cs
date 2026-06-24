using AIAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzerController : ControllerBase
    {
        private readonly ITestAnalysisService _analysisService;

        public AnalyzerController(ITestAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }





        [HttpPost("upload")]
        // Убрали IFormFile etalonFile, поменяли answersFile на массив List<IFormFile>
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

                // Передаем только ответы
                var result = _analysisService.Analyze(answersData);

                // Очищаем потоки
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
