using Azure.AI.OpenAI;
using OpenAI;
using PortfolioAI.Utils;
using System.Text.Json;

namespace PortfolioAI.Services
{
    public class ApiInitializer
    {
        private readonly DbCommands _dbCommands;
        private readonly OpenAIClient _client;
        private readonly IWebHostEnvironment _env;

        public ApiInitializer(DbCommands dbCommands, AzureOpenAIClient client, IWebHostEnvironment env)
        {
            _dbCommands = dbCommands;
            _client = client;
            _env = env;
        }

        public async Task InitializeAsync(string embeddingModel)
        {
            string dbPath = Path.Combine(_env.ContentRootPath, "AppData", "resume.db");
            string resumePath = Path.Combine(_env.ContentRootPath, "AppData", "Sandhya_Arcot.docx");

            if (File.Exists(dbPath))
            {
                Console.WriteLine("✅ resume.db already exists. Skipping creation.");
                return;
            }

            Console.WriteLine("🧠 Creating new resume.db...");

            _dbCommands.CreateResumeDb();

            // Split resume into chunks
            var chunks = ResumeChunker.SplitResumeIntoChunks(resumePath);
            var embeddingClient = _client.GetEmbeddingClient(embeddingModel);

            foreach (var chunk in chunks)
            {
                var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(chunk);
                float[] vector = embeddingResponse.Value.ToFloats().ToArray();
                byte[] blob = JsonSerializer.SerializeToUtf8Bytes(vector);
                _dbCommands.InsertChunk(chunk, blob);
            }

            Console.WriteLine("✅ resume.db successfully created!");
        }
    }
}
