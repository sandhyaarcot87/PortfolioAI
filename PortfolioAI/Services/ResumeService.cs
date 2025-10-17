namespace PortfolioAI.Services
{
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Http;
    using OpenXmlPowerTools;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

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
        var filePath = Path.Combine(_env.ContentRootPath, "AppData", "Sandhya_Resume_Full_Enhanced.docx");

        if (!File.Exists(filePath))
            return Results.NotFound(new { message = "Resume file not found." });

        var tempFile = Path.GetTempFileName();
        File.Copy(filePath, tempFile, true);

        string html;
        using (var doc = WordprocessingDocument.Open(tempFile, true))
        {
                var settings = new HtmlConverterSettings()
                {
                    PageTitle = "Resume",
                    FabricateCssClasses = true,
                    RestrictToSupportedLanguages = false,
                    RestrictToSupportedNumberingFormats = false
                };

                var htmle = HtmlConverter.ConvertToHtml(doc, settings);
                html = htmle.ToStringNewLineOnAttributes();
            }
            File.Delete(tempFile);

        // Extract top-level sections
        string summary = ExtractHtmlSection(html, "ABOUT ME", "AREAS OF EXPERTISE");
        string skills = ExtractHtmlSection(html, "AREAS OF EXPERTISE", "WORK EXPERIENCE");
        string workExperienceHtml = ExtractHtmlSection(html, "WORK EXPERIENCE", "EDUCATION &amp; CERTIFICATIONS");
        string education = ExtractHtmlSection(html, "EDUCATION &amp; CERTIFICATIONS", "");

        var workExperiences = ExtractWorkExperienceSectionsAuto(workExperienceHtml);

        var a = Results.Json(new { summary, skills, education, workExperiences });
            return a;
    }

        private string ExtractHtmlSection(string html, string sectionStart, string nextSection)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //p | //div | //ul | //ol");
        if (nodes == null) return "";

        var sb = new StringBuilder();
        bool insideSection = false;

        foreach (var node in nodes)
        {
            if (node.Name.StartsWith("h", StringComparison.OrdinalIgnoreCase))
            {
                var headerText = node.InnerText.Trim();

                if (insideSection && headerText.Equals(nextSection, StringComparison.OrdinalIgnoreCase))
                    break;

                if (headerText.Equals(sectionStart, StringComparison.OrdinalIgnoreCase))
                {
                    insideSection = true;
                    continue;
                }
            }

            if (insideSection)
                sb.Append(node.OuterHtml);
        }

        return sb.Length > 0 ? $"<div>{sb}</div>" : "";
    }

        private List<object> ExtractWorkExperienceSectionsAuto(string workHtml)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(workHtml);

            var nodes = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //p | //div | //ul | //ol");
            if (nodes == null)
                return new List<object>();

            var results = new List<object>();
            StringBuilder sb = null;
            string currentCompany = null;
            string currentRole = null;

            foreach (var node in nodes)
            {
                bool isHeader = false;
                string cleanText = node.InnerText.Trim();
                cleanText = cleanText.Replace("", "").Replace("•", "").Trim();
                // Detect potential new company section
                if (node.Name.Equals("h2", StringComparison.OrdinalIgnoreCase) ||
                    node.Name.Equals("h3", StringComparison.OrdinalIgnoreCase))
                {
                    isHeader = true;
                }
               
                // When we find a new section, save the previous
                if (isHeader)
                {
                    if (sb != null && !string.IsNullOrEmpty(currentCompany))
                    {
                        results.Add(new
                        {
                            company = currentCompany,
                            role = currentRole ?? "",
                            html = $"<div>{sb}</div>"
                        });
                        sb = null;
                    }

                    // Parse out company and role from same line if possible
                    currentCompany = cleanText;
                    currentRole = null;
                    sb = new StringBuilder();
                    continue;
                }

                if (sb != null)
                {
                    // Attempt to parse known info types
                     if (currentRole == null && cleanText.Length < 80 && !cleanText.Contains("•"))
                        currentRole = cleanText;
                    else
                        sb.Append(node.OuterHtml);
                }
            }

            // Final section
            if (sb != null && !string.IsNullOrEmpty(currentCompany))
            {
                results.Add(new
                {
                    company = currentCompany,
                    role = currentRole ?? "",
                    html = $"<div>{sb}</div>"
                });
            }

            // Optional: extract bullet points cleanly
            foreach (dynamic r in results)
            {
                // Convert HTML list items to text array
                var innerDoc = new HtmlDocument();
                innerDoc.LoadHtml(r.html);
                var bullets = innerDoc.DocumentNode.SelectNodes("//li")?.Select(li => li.InnerText.Trim()).ToList() ?? new List<string>();

                r.GetType().GetProperty("bullets")?.SetValue(r, bullets);
            }

            return results;
        }

    }

}
