namespace AIAnalyzer.Models
{
    public class LlmProvider
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } // Например: "Локальная Llama 3" или "Groq Cloud"
        public bool IsLocal { get; set; } // true — локальная (Ollama), false — облако по API
        public string ApiUrl { get; set; }
        public string ModelName { get; set; } // Например: "llama3"
        public string ApiKey { get; set; } // Для локальной можно оставлять пустым
        public string? Scope { get; set; }//для сбера
    }
}
