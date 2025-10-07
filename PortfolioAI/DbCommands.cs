using System.Text.Json;

namespace PortfolioAI;

    using Microsoft.Data.Sqlite;

    public class DbCommands
    {

        public void CreateResumeDb()
        {
            using var conn = new SqliteConnection("Data Source= AppData/resume.db");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ResumeChunks (Id INTEGER PRIMARY KEY AUTOINCREMENT,Content TEXT NOT NULL,Embedding BLOB NOT NULL);";
            cmd.ExecuteNonQuery();
        }

        public void InsertChunk(string chunkText, byte[] embedding)
        {

            using var conn = new SqliteConnection("Data Source= AppData/resume.db");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO ResumeChunks (Content, Embedding) VALUES ($content, $embedding)";
            cmd.Parameters.AddWithValue("$content", chunkText);
            cmd.Parameters.AddWithValue("$embedding", embedding);
            cmd.ExecuteNonQuery();
        }
}

