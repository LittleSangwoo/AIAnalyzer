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
        public async Task<IActionResult> UploadFiles([FromForm] IFormFile etalonFile, [FromForm] IFormFile answersFile)
        {
            if (etalonFile == null || answersFile == null)
                return BadRequest("Необходимо загрузить оба файла (Эталон и Ответы).");

            try
            {
                using var etalonStream = etalonFile.OpenReadStream();
                using var answersStream = answersFile.OpenReadStream();

                // Передаем и потоки, и имена файлов для определения расширения
                var result = _analysisService.Analyze(
                    etalonStream, etalonFile.FileName,
                    answersStream, answersFile.FileName
                );

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Ошибка при обработке файлов: {ex.Message}");
            }
        }
    }
}
