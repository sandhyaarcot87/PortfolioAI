namespace PortfolioAI.Services
{
    using Azure.AI.OpenAI;
    using Microsoft.Data.Sqlite;
    using OpenAI;
    using System.Text.Json;

    public class EmbeddingService
    {
        private readonly OpenAIClient _client;

        public EmbeddingService(AzureOpenAIClient client)
        {
            _client = client;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, string embeddings)
        {
            var embeddingClient = _client.GetEmbeddingClient(embeddings);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(text);
            return embeddingResponse.Value.ToFloats().ToArray();
        }

        public IEnumerable<(string content, float[] embedding)> LoadResumeEmbeddings(string dbPath)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Content, Embedding FROM ResumeChunks";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string content = reader.GetString(0);
                byte[] blob = (byte[])reader["Embedding"];
                float[] embedding = JsonSerializer.Deserialize<float[]>(blob)!;
                yield return (content, embedding);
            }
        }
    }

}
