using System.Windows;
using System.Text.Json.Serialization;

namespace CloudFG
{
    public class Document
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        [JsonPropertyName("date")]
        public string? Date { get; set; }
        [JsonPropertyName("size_bytes")]
        public long SizeBytes { get; set; }
        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        public Document() { }
        public Document(string hash, string title, string author, string date, long sizeBytes, string filePath)
        {
            Hash = hash;
            Title = title;
            Author = author;
            Date = date;
            SizeBytes = sizeBytes;
            FilePath = filePath;
        }
    }
}
