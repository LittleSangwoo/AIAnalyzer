using AIAnalyzer.ViewModels;
using System.Collections.Generic;
using System.IO;

namespace AIAnalyzer.Services
{
    public interface ITestAnalysisService
    {
        // список файлов с ответами
        AnalysisResultViewModel Analyze(List<(Stream stream, string fileName)> answersData);
    }
}