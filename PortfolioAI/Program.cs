using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;

using Microsoft.Data.Sqlite;
using OpenAI.Chat;
using PortfolioAI;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json or environment variables
string endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
string key = builder.Configuration["AzureOpenAI:ApiKey"];
string deployment = builder.Configuration["AzureOpenAI:DeploymentName"];
string embedding = builder.Configuration["AzureOpenAI:Embedding"];


var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var dbCommands = new DbCommands();

// In Program.cs
if (!File.Exists("AppData/resume.db"))
{
    Console.WriteLine("Creating new resume.db...");
    dbCommands.CreateResumeDb();
    var chunks = ResumeChunker.SplitResumeIntoChunks("AppData/Sandhya_Arcot.docx");
        var embeddedClient = client.GetEmbeddingClient(embedding);

        foreach (var chunk in chunks)
        {
            var embeddingResponse = await embeddedClient.GenerateEmbeddingAsync(chunk);

            float[] vector = embeddingResponse.Value.ToFloats().ToArray();
            byte[] blob = JsonSerializer.SerializeToUtf8Bytes(vector);
            dbCommands.InsertChunk(chunk, blob);

        }

}
else
{
    Console.WriteLine("resume.db already exists. Skipping creation.");
}





app.MapPost("/api/chat", async (ChatRequest req) =>
{
    using var conn = new SqliteConnection("Data Source=AppData/resume.db");
    conn.Open();

    // Step 1: Embed user question
    var embeddedClient = client.GetEmbeddingClient("resume-embeddings");
    var embeddingResponse = await embeddedClient.GenerateEmbeddingAsync(req.Message);
    float[] queryVector = embeddingResponse.Value.ToFloats().ToArray();

    // Step 2: Load resume embeddings from DB
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Content, Embedding FROM ResumeChunks";
    using var reader = cmd.ExecuteReader();

    var scoredChunks = new List<(string content, float score)>();

    while (reader.Read())
    {
        string content = reader.GetString(0);
        byte[] blob = (byte[])reader["Embedding"];
        float[] storedVector = JsonSerializer.Deserialize<float[]>(blob)!;

        float score = UtilityClass.CosineSimilarityA(queryVector, storedVector);
        if (score > 0.75f)
            scoredChunks.Add((content, score));
    }


    string prompt;
    // Step 3: Pick top 3
    if (scoredChunks.Count == 0)
    {
        prompt = "You are an assistant that only answers questions about Sandhya's resume. " +
                 "The question is unrelated to her experience, so reply: 'I can only answer questions about Sandhya’s professional experience.'";
    }
    else
    {
        string context = string.Join("\n", scoredChunks.Select(x => x.content));
        prompt = $"You are an assistant that answers questions about Sandhya's resume. " +
                 $"Use the following context to answer the question in third person:\n{context}\nQuestion: {req.Message}";
    }

    //  var topChunks = ;
    var chatClient = client.GetChatClient(deployment);
    // Step 4: Call Chat model with context
    var messages = new ChatMessage[]
    {
        new SystemChatMessage("You are an assistant that details resume information in third person."),
        new UserChatMessage(prompt),
    };

    var chatResponse = await chatClient.CompleteChatAsync(messages);

    // Get the raw text
    string rawText = chatResponse.Value.Content[0].Text;

    // Return JSON explicitly
    return Results.Json(new { reply = rawText });
});

app.MapGet("/download-resume", async (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "AppData", "Sandhya_Arcot.docx");

    if (!File.Exists(filePath))
        return Results.NotFound("Resume not found.");

    var fileBytes = await File.ReadAllBytesAsync(filePath);

    // This tells the browser to download the file
    return Results.File(fileBytes,
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "Sandhya_Resume.docx");
});


app.UseDefaultFiles();
app.UseStaticFiles();
//app.UseSwagger();
//app.UseSwaggerUI();
app.Run();
record ChatRequest(string Message);
