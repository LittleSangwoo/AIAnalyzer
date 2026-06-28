using AIAnalyzer.Models;
using System.Text.Json;

namespace AIAnalyzer.Services
{
    public class LlmSettingsService
    {
        private readonly string _filePath = "llm_providers.json";
        private readonly object _lock = new object(); 

        public List<LlmProvider> GetAllProviders()
        {
            if (!File.Exists(_filePath)) return new List<LlmProvider>();

            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<LlmProvider>>(json) ?? new List<LlmProvider>();
            }
        }

        public void AddProvider(LlmProvider provider)
        {
            var providers = GetAllProviders();
            providers.Add(provider);
            SaveProviders(providers);
        }

        public void DeleteProvider(string id)
        {
            var providers = GetAllProviders();
            providers.RemoveAll(p => p.Id == id);
            SaveProviders(providers);
        }

        private void SaveProviders(List<LlmProvider> providers)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(providers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }
    }
}
