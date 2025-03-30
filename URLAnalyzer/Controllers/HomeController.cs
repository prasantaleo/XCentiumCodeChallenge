using Microsoft.AspNetCore.Mvc;
using URLAnalyzer.Services;


namespace URLAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUrlAnalyzerService _urlAnalyzerService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUrlAnalyzerService urlAnalyzerService, ILogger<HomeController> logger)
        {
            _urlAnalyzerService = urlAnalyzerService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("AnalyzeUrlAsync called with empty URL.");
                return BadRequest("URL cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Starting URL analysis for: {Url}", url);
                var result = await _urlAnalyzerService.AnalyzeUrlAsync(url);
                _logger.LogInformation("URL analysis completed for: {Url}", url);

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing the URL: {Url}", url);
                return BadRequest("Error analyzing the URL.");
            }
        }
    }
}



