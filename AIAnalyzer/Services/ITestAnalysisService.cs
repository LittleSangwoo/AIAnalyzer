using AIAnalyzer.ViewModels;

namespace AIAnalyzer.Services
{
    public interface ITestAnalysisService
    {
        AnalysisResultViewModel Analyze(Stream etalonStream, Stream answersStream);
    }
}
