namespace CT600Submission.Models;

public class SubmissionStatus
{
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StatusDate { get; set; }
    public string? Message { get; set; }
}
