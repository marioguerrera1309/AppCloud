using System.Windows;
using System.Text.Json.Serialization;
namespace CloudFG
{
    public class Analitics
    {       
        [JsonPropertyName("hash")] 
        public string Hash { get; set; }
        [JsonPropertyName("gulpease_index")]
        public float GulpeaseIndex { get; set; }
        [JsonPropertyName("letters")]
        public int Letters { get; set; }
        [JsonPropertyName("words")]
        public int Words { get; set; }
        [JsonPropertyName("sentences")]
        public int Sentences { get; set; }
        [JsonPropertyName("read_time")]
        public float ReadTime {get; set;}
        [JsonPropertyName("time_analysis")]
        public float TimeAnalysis {get; set;}
        //Questo dice a C# che è accettabile che quei valori siano inizialmente vuoti mentre il sistema "spacchetta" il JSON che arriva da Go.
        [JsonPropertyName("unique_words")]
        public int UniqueWords { get; set; }
        
        public Analitics() { }  //costruttore vuoto necessario per il json
        public Analitics(string hash, float gulpeaseIndex, int letters, int words, int sentences, float readTime, float timeAnalysis, int uniqueWords)
        {
            Hash = hash;
            GulpeaseIndex = gulpeaseIndex;
            Letters = letters;
            Words = words;
            Sentences = sentences;
            ReadTime = readTime;
            TimeAnalysis = timeAnalysis;
            UniqueWords = uniqueWords;
        }
    }
}
