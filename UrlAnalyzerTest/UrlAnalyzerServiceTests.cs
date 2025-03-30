using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Threading.Tasks;
using URLAnalyzer.Models;
using URLAnalyzer.Services;
using Xunit;

namespace UrlAnalyzerTest
{
    public class UrlAnalyzerServiceTests
    {
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly IUrlAnalyzerService _urlAnalyzerService;

        public UrlAnalyzerServiceTests()
        {
            _mockMemoryCache = new Mock<IMemoryCache>();
            _urlAnalyzerService = new UrlAnalyzerService(_mockMemoryCache.Object);
        }

        [Fact]
        public async Task AnalyzeUrlAsync_CachedResult_ReturnsCachedResult()
        {
            // Arrange
            var url = "http://example.com";
            var cachedResult = new UrlAnalysisResultModel
            {
                ImageUrls = new List<string> { "http://example.com/image1.jpg" },
                WordCount = 100,
                TopWords = new Dictionary<string, int> { { "example", 10 } }
            };
            object cacheEntry = cachedResult;
            _mockMemoryCache.Setup(cache => cache.TryGetValue(url, out cacheEntry)).Returns(true);

            // Act
            var result = await _urlAnalyzerService.AnalyzeUrlAsync(url);

            // Assert
            Assert.Equal(cachedResult, result);
        }


        [Fact]
        public async Task AnalyzeUrlAsync_ValidUrl_ReturnsAnalysisResult()
        {
            // Arrange
            var url = "http://example.com";
            var cache = new MemoryCache(new MemoryCacheOptions());

            // Mock the GetHtmlContentsFromUrlAsync method to return a sample HTML content
            var htmlContent = "<html><body><img src='http://example.com/image1.jpg' /><p>Example text with some words.</p></body></html>";
            var mockUrlAnalyzerService = new Mock<UrlAnalyzerService>(cache) { CallBase = true };
            mockUrlAnalyzerService.Setup(service => service.GetHtmlContentsFromUrlAsync(url)).ReturnsAsync(htmlContent);

            // Act
            var result = await mockUrlAnalyzerService.Object.AnalyzeUrlAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.ImageUrls);
            Assert.True(result.WordCount > 0);
            Assert.NotEmpty(result.TopWords);
            Assert.Contains("http://example.com/image1.jpg", result.ImageUrls);
            Assert.True(result.TopWords.ContainsKey("example"));
        }




        [Fact]
        public async Task AnalyzeUrlAsync_InvalidUrl_ThrowsException()
        {
            // Arrange
            var url = "invalid-url";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _urlAnalyzerService.AnalyzeUrlAsync(url));
        }
    }
}
