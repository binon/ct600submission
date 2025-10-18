using Microsoft.AspNetCore.Mvc;
using CT600Submission.Models;
using CT600Submission.Services;

namespace CT600Submission.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CT600Controller : ControllerBase
{
    private readonly GoogleSheetsService _sheetsService;
    private readonly HmrcService _hmrcService;
    private readonly ILogger<CT600Controller> _logger;

    public CT600Controller(
        GoogleSheetsService sheetsService,
        HmrcService hmrcService,
        ILogger<CT600Controller> logger)
    {
        _sheetsService = sheetsService;
        _hmrcService = hmrcService;
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes a string to prevent log forging attacks by removing newline characters
    /// </summary>
    private static string SanitizeForLogging(string input)
    {
        return input?.Replace("\n", "").Replace("\r", "") ?? string.Empty;
    }

    /// <summary>
    /// Get all CT600 data from Google Sheets
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CT600Data>>> GetAll()
    {
        try
        {
            var data = await _sheetsService.GetAllDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CT600 data");
            return StatusCode(500, new { error = "Failed to retrieve data", message = ex.Message });
        }
    }

    /// <summary>
    /// Get CT600 data by tax reference
    /// </summary>
    [HttpGet("{taxReference}")]
    public async Task<ActionResult<CT600Data>> GetByReference(string taxReference)
    {
        try
        {
            var data = await _sheetsService.GetDataByReferenceAsync(taxReference);
            if (data == null)
            {
                return NotFound(new { error = "CT600 data not found for the given tax reference" });
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CT600 data for reference {Reference}", SanitizeForLogging(taxReference));
            return StatusCode(500, new { error = "Failed to retrieve data", message = ex.Message });
        }
    }

    /// <summary>
    /// Update CT600 data
    /// </summary>
    [HttpPut("{taxReference}")]
    public async Task<ActionResult> Update(string taxReference, [FromBody] CT600Data data)
    {
        try
        {
            if (taxReference != data.TaxReference)
            {
                return BadRequest(new { error = "Tax reference in URL does not match data" });
            }

            var success = await _sheetsService.UpdateDataAsync(taxReference, data);
            if (!success)
            {
                return NotFound(new { error = "CT600 data not found for the given tax reference" });
            }

            return Ok(new { message = "CT600 data updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CT600 data for reference {Reference}", SanitizeForLogging(taxReference));
            return StatusCode(500, new { error = "Failed to update data", message = ex.Message });
        }
    }

    /// <summary>
    /// Add new CT600 data
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Add([FromBody] CT600Data data)
    {
        try
        {
            var success = await _sheetsService.AddDataAsync(data);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to add CT600 data" });
            }

            return CreatedAtAction(nameof(GetByReference), new { taxReference = data.TaxReference }, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding CT600 data");
            return StatusCode(500, new { error = "Failed to add data", message = ex.Message });
        }
    }

    /// <summary>
    /// Submit CT600 data to HMRC
    /// </summary>
    [HttpPost("{taxReference}/submit")]
    public async Task<ActionResult> Submit(string taxReference)
    {
        try
        {
            if (!_hmrcService.IsAuthenticated())
            {
                return Unauthorized(new { error = "Not authenticated with HMRC. Please authenticate first." });
            }

            var data = await _sheetsService.GetDataByReferenceAsync(taxReference);
            if (data == null)
            {
                return NotFound(new { error = "CT600 data not found for the given tax reference" });
            }

            var submissionReference = await _hmrcService.SubmitCT600Async(data);

            // Update the data with submission information
            data.Status = "Submitted";
            data.SubmissionDate = DateTime.UtcNow;
            data.SubmissionReference = submissionReference;
            await _sheetsService.UpdateDataAsync(taxReference, data);

            return Ok(new 
            { 
                message = "CT600 submitted successfully",
                submissionReference = submissionReference,
                submissionDate = data.SubmissionDate
            });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting CT600 for reference {Reference}", SanitizeForLogging(taxReference));
            return StatusCode(500, new { error = "Failed to submit CT600", message = ex.Message });
        }
    }

    /// <summary>
    /// Get submission status from HMRC
    /// </summary>
    [HttpGet("{taxReference}/status")]
    public async Task<ActionResult<SubmissionStatus>> GetStatus(string taxReference)
    {
        try
        {
            if (!_hmrcService.IsAuthenticated())
            {
                return Unauthorized(new { error = "Not authenticated with HMRC. Please authenticate first." });
            }

            var data = await _sheetsService.GetDataByReferenceAsync(taxReference);
            if (data == null)
            {
                return NotFound(new { error = "CT600 data not found for the given tax reference" });
            }

            if (string.IsNullOrEmpty(data.SubmissionReference))
            {
                return BadRequest(new { error = "No submission reference found. CT600 has not been submitted yet." });
            }

            var status = await _hmrcService.GetSubmissionStatusAsync(data.SubmissionReference);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission status for reference {Reference}", SanitizeForLogging(taxReference));
            return StatusCode(500, new { error = "Failed to get submission status", message = ex.Message });
        }
    }
}
