using System.Windows;
using System.Text.Json.Serialization;
namespace CloudFG
{
    public class Document
    {
        [JsonPropertyName("hash")]// Attributo per il mapping della proprietà nel JSON al nome della proprietà C#
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
        //? dice al compilatore che è accettabile che quei valori siano inizialmente null mentre il sistema "spacchetta" il JSON che arriva da Go.
        public Document() { }  //costruttore vuoto necessario per il json, la libreria System.Text.Json crea prima l'oggetto vuota e poi popola le proprietà con i dati del JSON.
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
