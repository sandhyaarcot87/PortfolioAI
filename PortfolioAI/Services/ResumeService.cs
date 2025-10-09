namespace PortfolioAI.Services
{
    using DocumentFormat.OpenXml.Packaging;
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

        public async Task<IResult> GetSections()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "AppData", "Sandhya_Arcot.docx");

            if (!File.Exists(filePath))
                return Results.NotFound(new { message = "Resume file not found." });

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart.Document.Body;
            var text = body.InnerText;

            // Split resume by keywords
            string summary = ExtractSection(text, "SUMMARY OF QUALIFICATIONS", new[] { "AREAS OF EXPERTISE" });
            string skills = ExtractSection(text, "AREAS OF EXPERTISE", new[] { "WORK EXPERIENCE" });
            string projects = ExtractSection(text, "WORK EXPERIENCE", new[] { "Education & CERTIFICATIONS" });
            string education = ExtractSection(text, "EDUCATION & CERTIFICATIONS", new[] { "Education & CERTIFICATIONS" });


            return Results.Json(new { summary, projects, skills, education });
        }

        private static string ExtractSection(string text, string sectionStart, string[] nextSections)
        {
            int start = text.IndexOf(sectionStart, StringComparison.OrdinalIgnoreCase);
            if (start == -1) return "";

            int end = nextSections
                .Select(s => text.IndexOf(s, start + sectionStart.Length, StringComparison.OrdinalIgnoreCase))
                .Where(i => i != -1)
                .DefaultIfEmpty(text.Length)
                .Min();

            string sectionText = text.Substring(start, end - start);
            return sectionText.Replace("\r", "<br>").Replace("\n", "<br>");
        }

    }

}
