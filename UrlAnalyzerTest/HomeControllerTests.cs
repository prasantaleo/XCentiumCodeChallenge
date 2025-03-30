using URLAnalyzer.Controllers;
using URLAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using URLAnalyzer.Models;

namespace UrlAnalyzerTest
{
    public class HomeControllerTests
    {
        private readonly Mock<IUrlAnalyzerService> _mockUrlAnalyzerService;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockUrlAnalyzerService = new Mock<IUrlAnalyzerService>();
            _controller = new HomeController(_mockUrlAnalyzerService.Object);
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AnalyzeUrlAsync_EmptyUrl_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.AnalyzeUrlAsync(string.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("URL cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeUrlAsync_ValidUrl_ReturnsJsonResult()
        {
            // Arrange
            var url = "http://example.com";
            var analysisResult = new UrlAnalysisResultModel
            {
                ImageUrls = new List<string> { "http://example.com/image1.jpg" },
                WordCount = 100,
                TopWords = new Dictionary<string, int> { { "example", 10 } }
            };
            _mockUrlAnalyzerService.Setup(service => service.AnalyzeUrlAsync(url))
                                   .ReturnsAsync(analysisResult);

            // Act
            var result = await _controller.AnalyzeUrlAsync(url);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(analysisResult, jsonResult.Value);
        }

        [Fact]
        public async Task AnalyzeUrlAsync_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var url = "http://example.com";
            _mockUrlAnalyzerService.Setup(service => service.AnalyzeUrlAsync(url))
                                   .ThrowsAsync(new System.Exception());

            // Act
            var result = await _controller.AnalyzeUrlAsync(url);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error analyzing the URL.", badRequestResult.Value);
        }
    }
}
