using AIAnalyzer.ViewModels;
using System.Collections.Generic;
using System.IO;

namespace AIAnalyzer.Services
{
    public interface ITestAnalysisService
    {
        AnalysisResultViewModel Analyze(List<(Stream stream, string fileName)> answersData);
    }
}