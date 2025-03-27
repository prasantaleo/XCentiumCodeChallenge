using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;

using URLAnalyzer.Models;


namespace URLAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        //private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public HomeController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
           _cache = cache;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AnalyzeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest("URL cannot be empty.");

            // Check if data is cached
            if (_cache.TryGetValue(url, out UrlAnalysisResultModel cachedResult))
            {
                return Json(cachedResult);
            }

            var result = new UrlAnalysisResultModel();

            try
            {

                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
                });
                using var page = await browser.NewPageAsync();

                await page.GoToAsync(url, WaitUntilNavigation.Load);
                //var html = await page.GetContentAsync();

                // Extract Images
                var imageUrls = await page.EvaluateFunctionAsync<string[]>(
                    @"() => {
        return Array.from(document.querySelectorAll('img, source'))
            .map(img => img.src || img.dataset?.src)
            .filter(src => src && src.length > 0);
    }
");

                // Convert relative URLs to absolute and remove duplicates
                var baseUri = new Uri(url);
                result.ImageUrls = imageUrls.Distinct()
                    .Select(src => Uri.TryCreate(src, UriKind.Absolute, out var absUri)
                        ? absUri.ToString()
                        : new Uri(baseUri, src).ToString())
                    .ToList();


                // Extract words and count occurrences
                var text = await page.EvaluateFunctionAsync<string>(@"
    () => document.body.innerText
");

                var words = text.Split(new[] { ' ', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                result.WordCount = words.Length;

                var wordGroups = words.GroupBy(w => w.ToLower())
                                      .Select(g => new { Word = g.Key, Count = g.Count() })
                                      .OrderByDescending(g => g.Count)
                                      .Take(10);

                foreach (var group in wordGroups)
                    result.TopWords[group.Word] = group.Count;

                // Cache the result for 10 minutes
                _cache.Set(url, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(10)
                });

                return Json(result);

            }
            catch
            {
                return BadRequest("Error analyzing the URL.");
            }
        }
    }
}



