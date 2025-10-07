using Azure;
using Azure.AI.OpenAI;
using PortfolioAI.EndPoints;
using PortfolioAI.Services;
using PortfolioAI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json or environment variables
string endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
string key = builder.Configuration["AzureOpenAI:ApiKey"];
string deployment = builder.Configuration["AzureOpenAI:DeploymentName"];
string embedding = builder.Configuration["AzureOpenAI:Embedding"];


var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
builder.Services.AddSingleton(client);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DbCommands>();
builder.Services.AddSingleton<ApiInitializer>();
builder.Services.AddSingleton<ResumeService>();
builder.Services.AddSingleton<EmbeddingService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ApiInitializer>();
    await initializer.InitializeAsync(embedding);
}

app.MapChatEndpoints(deployment, embedding);


app.UseDefaultFiles();
app.UseStaticFiles();
//app.UseSwagger();
//app.UseSwaggerUI();
app.Run();
record ChatRequest(string Message);
