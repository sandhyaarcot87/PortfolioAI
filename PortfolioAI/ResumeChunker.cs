using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace PortfolioAI;

public class ResumeChunker
{
    public static List<string> SplitResumeIntoChunks(string filePath, int maxWordsPerChunk = 50)
    {
        // Extract text from DOCX
        string text;
        using (var doc = WordprocessingDocument.Open(filePath, false))
        {
            var body = doc.MainDocumentPart.Document.Body;
            text = body.InnerText;
        }

        // Split sentences on ., !, ?
        var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var chunks = new List<string>();
        var currentChunk = new List<string>();
        int wordCount = 0;

        foreach (var sentence in sentences)
        {
            int sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (wordCount + sentenceWords > maxWordsPerChunk && currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk.Clear();
                wordCount = 0;
            }

            currentChunk.Add(sentence);
            wordCount += sentenceWords;
        }

        if (currentChunk.Count > 0)
            chunks.Add(string.Join(" ", currentChunk));

        return chunks;
    }
}