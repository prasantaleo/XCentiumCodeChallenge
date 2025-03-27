namespace URLAnalyzer.Models
{
    public class UrlAnalysisResultModel
    {
        public List<string> ImageUrls { get; set; } = new List<string>();
        public int WordCount { get; set; }
        public Dictionary<string, int> TopWords { get; set; } = new Dictionary<string, int>();

    }
}
