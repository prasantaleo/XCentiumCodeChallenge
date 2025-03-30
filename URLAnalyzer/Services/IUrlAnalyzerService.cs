using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using URLAnalyzer.Models;

namespace URLAnalyzer.Services
{
    public interface IUrlAnalyzerService
    {
        Task<UrlAnalysisResultModel> AnalyzeUrlAsync(string url);
    }

    public class UrlAnalyzerService : IUrlAnalyzerService
    {
        const int mostOccurringWordsCount = 10;
        const int cacheAgeInMinutes = 10;
        private readonly IMemoryCache _cache;

        public UrlAnalyzerService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Scrap images and words from an URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<UrlAnalysisResultModel> AnalyzeUrlAsync(string url)
        {
            // Check if the URL is valid
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException("The provided URL is not valid.", nameof(url));
            }

            // Check if data is cached
            if (_cache.TryGetValue(url, out UrlAnalysisResultModel cachedResult))
            {
                return cachedResult;
            }

            var result = new UrlAnalysisResultModel();
            var htmlContents = await GetHtmlContentsFromUrlAsync(url);

            var imageListTask = Task.Run(() => GetImageUrlsWithBaseUri(url, htmlContents, result));
            var wordsCountTask = Task.Run(() => GetWordFrequency(result, htmlContents, mostOccurringWordsCount));

            await Task.WhenAll(imageListTask, wordsCountTask);

            _cache.Set(url, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheAgeInMinutes)
            });

            return result;
        }


        /// <summary>
        /// Get HTML contents from the url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual async Task<string> GetHtmlContentsFromUrlAsync(string url)
        {
            // Create an instance of HttpClient
            HttpClient httpClient = new HttpClient();

            string customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36";
            httpClient.DefaultRequestHeaders.Add("User-Agent", customUserAgent);

            var htmlContents = await httpClient.GetStringAsync(url).ConfigureAwait(false); 
            return htmlContents;
        }

        /// <summary>
        /// Extracts image urls from HTML content
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        private static List<string> ExtractImageUrls(string pageUrl, string htmlContent)
        {
            //var urls = new List<string>();
            //var regex = new Regex("<img[^>]+?src=[\"'](?<url>.*?)[\"'][^>]*>", RegexOptions.IgnoreCase);
            //var matches = regex.Matches(htmlContent).Distinct();
            //foreach (Match match in matches)
            //{
            //    urls.Add(match.Groups["ürl"].Value);
            //}

            List<string> imageUrls = new List<string>();
            Regex regex = new Regex("<img[^>]+src=[\"']?([^\"'>]+)[\"']?", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(htmlContent);

            foreach (Match match in matches)
            {
                string imageUrl = match.Groups[1].Value;
                if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    Uri baseUri = new Uri(pageUrl);
                    Uri absoluteUri = new Uri(baseUri, imageUrl);
                    imageUrl = absoluteUri.ToString();
                }
                imageUrls.Add(imageUrl);
            }

            return imageUrls;
        }

        /// <summary>
        /// Append Image URLs with Base URI
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <param name="result"></param>
        /// <param name="imageUrlList"></param>
        private static void GetImageUrlsWithBaseUri(string pageUrl, string htmlContents, UrlAnalysisResultModel result)
        {
            List<string> imageUrlList = ExtractImageUrls(pageUrl, htmlContents);
             
            result.ImageUrls = imageUrlList;
        }


        /// <summary>
        /// Extracts words from HTML content
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        private static string[] ExtractWordsFromHtmlContents(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var htmlDocumentText = doc.DocumentNode.InnerText;
            //var words = Regex.Split(htmlDocumentText, @"\W+", RegexOptions.IgnorePatternWhitespace);
            //var words = htmlContent.Split(new[] { ' ', '\n', '\t', '\r', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            char[] wordsDelimiter = new char[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':', '!', '?', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'' };
            var words = htmlDocumentText.Split(wordsDelimiter, StringSplitOptions.RemoveEmptyEntries);

            return words;
        }

        /// <summary>
        /// Get total word count and top occurring words in the result 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="htmlContent"></param>
        /// <param name="maxElementsCount"></param>
        private void GetWordFrequency(UrlAnalysisResultModel result, string htmlContent, int maxElementsCount)
        {
            var words = ExtractWordsFromHtmlContents(htmlContent);
            result.WordCount = words.Length;

            var wordGroups = words.GroupBy(w => w.ToLower())
                             .Select(g => new { Word = g.Key, Occurrence = g.Count() })
                             .OrderByDescending(g => g.Occurrence)
                             .Take(maxElementsCount);

            foreach (var group in wordGroups)
            {
                result.TopWords[group.Word] = group.Occurrence;
            }
        }

    }
}
