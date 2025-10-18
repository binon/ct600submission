namespace CT600Submission.Models;

public class CT600Data
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyRegistrationNumber { get; set; } = string.Empty;
    public string TaxReference { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Turnover { get; set; }
    public decimal TaxableProfit { get; set; }
    public decimal TaxDue { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SubmissionDate { get; set; }
    public string? SubmissionReference { get; set; }
}
