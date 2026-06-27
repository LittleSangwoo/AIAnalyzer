using AIAnalyzer.Models;
using System.Text.Json;

namespace AIAnalyzer.Services
{
    public class LlmSettingsService
    {
        private readonly string _filePath = "llm_providers.json";
        private readonly object _lock = new object(); // Защита от одновременной записи разными юзерами

        // Получить все нейросети
        public List<LlmProvider> GetAllProviders()
        {
            if (!File.Exists(_filePath)) return new List<LlmProvider>();

            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<LlmProvider>>(json) ?? new List<LlmProvider>();
            }
        }

        // Добавить новую нейросеть
        public void AddProvider(LlmProvider provider)
        {
            var providers = GetAllProviders();
            providers.Add(provider);
            SaveProviders(providers);
        }

        // Удалить нейросеть
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
