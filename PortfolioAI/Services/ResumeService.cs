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
        var filePath = Path.Combine(_env.ContentRootPath, "AppData", "Sandhya_Arcot.docx");

        if (!File.Exists(filePath))
            return Results.NotFound(new { message = "Resume file not found." });

        var tempFile = Path.GetTempFileName();
        File.Copy(filePath, tempFile, true);

        string html;
        using (var doc = WordprocessingDocument.Open(tempFile, true))
        {
            var settings = new HtmlConverterSettings() { PageTitle = "Resume" };
            html = HtmlConverter.ConvertToHtml(doc, settings).ToString(SaveOptions.DisableFormatting);
        }
        File.Delete(tempFile);

        // Extract top-level sections
        string summary = ExtractHtmlSection(html, "SUMMARY OF QUALIFICATIONS", "AREAS OF EXPERTISE");
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

        var result = new List<(string Company, string Html)>();
        StringBuilder sb = null;
        string currentCompany = null;

        foreach (var node in nodes)
        {
            bool isPotentialCompanyHeader = false;
            string cleanText = node.InnerText.Trim();

            // Case 1: Explicit heading tags
            if (node.Name.Equals("h2", StringComparison.OrdinalIgnoreCase) ||
                node.Name.Equals("h3", StringComparison.OrdinalIgnoreCase))
            {
                isPotentialCompanyHeader = true;
            }
            // Case 2: Fully bold paragraphs (common for company names)
            else if (node.Name == "p" && Regex.IsMatch(node.InnerHtml, @"^<b>.*?</b>$", RegexOptions.IgnoreCase))
            {
                isPotentialCompanyHeader = true;
            }
            // Case 3: Paragraphs with ALL CAPS short text (like “MICROSOFT”)
            else if (node.Name == "p" && cleanText.Length > 2 &&
                     cleanText.Length < 50 && cleanText == cleanText.ToUpper())
            {
                isPotentialCompanyHeader = true;
            }

            if (isPotentialCompanyHeader)
            {
                // If we were collecting, save the previous one
                if (sb != null && !string.IsNullOrEmpty(currentCompany))
                {
                    result.Add(($"<div>{currentCompany}</div>", $"<div>{sb}</div>"));
                    sb = null;
                }

                currentCompany = cleanText;
                sb = new StringBuilder();
                continue;
            }

            if (sb != null)
                sb.Append(node.OuterHtml);
        }

        if (sb != null && !string.IsNullOrEmpty(currentCompany))
            result.Add(($"<div>{currentCompany}</div>", $" <div>{sb}</div>"));

            var workExperiences = new List<object>();
            foreach (var section in result)
            {
                workExperiences.Add(new
                {
                    company = section.Company,
                    html = section.Html
                });
            }

            return workExperiences;
    }


}

}
