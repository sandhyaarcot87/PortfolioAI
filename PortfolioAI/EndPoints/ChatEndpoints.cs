namespace PortfolioAI.EndPoints
{
    using Azure.AI.OpenAI;
    using DocumentFormat.OpenXml.Packaging;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using OpenAI;
    using OpenAI.Chat;
    using PortfolioAI.Services;
    using PortfolioAI.Utils;

    public static class ChatEndpoints
    {
        public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app, string deployment, string embedding)
        {
            app.MapPost("/api/chat", async (
                ChatRequest req,
                EmbeddingService embeddings,
                ResumeService resumeSvc,
                IServiceProvider sp) =>
            {
                string dbPath = resumeSvc.GetDatabasePath();

                // Step 1: Embed user query
                float[] queryVector = await embeddings.GenerateEmbeddingAsync(req.Message, embedding);

                // Step 2: Load embeddings from DB
                var scoredChunks = new List<(string content, float score)>();
                foreach (var (content, storedVector) in embeddings.LoadResumeEmbeddings(dbPath))
                {
                    float score = UtilityClass.CosineSimilarityA(queryVector, storedVector);
                    if (score > 0.75f)
                        scoredChunks.Add((content, score));
                }

                // Step 3: Prepare prompt
                string prompt;
                if (scoredChunks.Count == 0)
                {
                    prompt = "You are an assistant that only answers questions about Sandhya's resume. " +
                             "The question is unrelated to her experience, so reply: 'I can only answer questions about Sandhya’s professional experience.'";
                }
                else
                {
                    string context = string.Join("\n", scoredChunks.Select(x => x.content));
                    prompt = $"You are an assistant that answers questions about Sandhya's resume. " +
                             $"Use the following context to answer in third person:\n{context}\nQuestion: {req.Message}";
                }

                // Step 4: Chat response
                var openAiClient = sp.GetRequiredService<AzureOpenAIClient>();
                var chatClient = openAiClient.GetChatClient(deployment);

                var messages = new ChatMessage[]
                {
                new SystemChatMessage("You are an assistant that details resume information in third person."),
                new UserChatMessage(prompt),
                };

                var chatResponse = await chatClient.CompleteChatAsync(messages);
                string rawText = chatResponse.Value.Content[0].Text;

                return Results.Json(new { reply = rawText });
            });

            app.MapGet("/download-resume", (ResumeService resumeSvc) => resumeSvc.DownloadResumeAsync());
            app.MapGet("/api/resume-sections", (ResumeService resumeSvc) => resumeSvc.GetSections());
           
            return app;
        }

    }

}
