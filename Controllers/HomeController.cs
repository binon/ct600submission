using Microsoft.AspNetCore.Mvc;

namespace CT600Submission.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Main dashboard page
    /// </summary>
    public IActionResult Index()
    {
        ViewData["ApiBaseUrl"] = GetApiBaseUrl();
        return View();
    }

    /// <summary>
    /// CT600 detail/edit page
    /// </summary>
    public IActionResult Detail(string? id)
    {
        ViewData["ApiBaseUrl"] = GetApiBaseUrl();
        ViewData["TaxReference"] = id;
        return View();
    }

    /// <summary>
    /// Authentication page
    /// </summary>
    public IActionResult Auth()
    {
        ViewData["ApiBaseUrl"] = GetApiBaseUrl();
        return View();
    }

    private string GetApiBaseUrl()
    {
        // In production, this should come from configuration
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }
}
