namespace PortfolioAI.Services
{
    using Microsoft.AspNetCore.Http;

    public class ResumeService
    {
        private readonly IWebHostEnvironment _env;

        public ResumeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GetResumePath() =>
            Path.Combine(_env.ContentRootPath, "AppData", "Sandhya_Arcot.docx");

        public string GetDatabasePath() =>
            Path.Combine(_env.ContentRootPath, "AppData", "resume.db");

        public async Task<IResult> DownloadResumeAsync()
        {
            var filePath = GetResumePath();
            if (!File.Exists(filePath))
                return Results.NotFound("Resume not found.");

            var bytes = await File.ReadAllBytesAsync(filePath);
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Sandhya_Resume.docx");
        }
    }

}
