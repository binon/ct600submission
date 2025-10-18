using Microsoft.AspNetCore.Mvc;
using CT600Submission.Services;

namespace CT600Submission.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HmrcService _hmrcService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(HmrcService hmrcService, ILogger<AuthController> logger)
    {
        _hmrcService = hmrcService;
        _logger = logger;
    }

    /// <summary>
    /// Get HMRC authorization URL to start OAuth flow
    /// </summary>
    [HttpGet("authorize")]
    public async Task<ActionResult> GetAuthorizationUrl()
    {
        try
        {
            var authUrl = await _hmrcService.GetAuthorizationUrlAsync();
            return Ok(new 
            { 
                authorizationUrl = authUrl,
                message = "Visit this URL to authorize the application with HMRC"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL");
            return StatusCode(500, new { error = "Failed to generate authorization URL", message = ex.Message });
        }
    }

    /// <summary>
    /// OAuth callback endpoint to receive authorization code from HMRC
    /// </summary>
    [HttpGet("callback")]
    public async Task<ActionResult> Callback([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Authorization code is missing" });
            }

            var token = await _hmrcService.ExchangeCodeForTokenAsync(code);
            return Ok(new 
            { 
                message = "Successfully authenticated with HMRC",
                expiresIn = token.ExpiresIn,
                expiresAt = token.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback");
            return StatusCode(500, new { error = "Failed to complete authentication", message = ex.Message });
        }
    }

    /// <summary>
    /// Check authentication status
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetAuthStatus()
    {
        var isAuthenticated = _hmrcService.IsAuthenticated();
        return Ok(new 
        { 
            isAuthenticated = isAuthenticated,
            message = isAuthenticated ? "Authenticated with HMRC" : "Not authenticated with HMRC"
        });
    }
}
