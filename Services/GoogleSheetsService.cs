using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using CT600Submission.Models;

namespace CT600Submission.Services;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;
    private readonly ILogger<GoogleSheetsService> _logger;
    private const string Range = "CT600Data!A2:J";

    public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
    {
        _logger = logger;
        _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"] ?? throw new ArgumentNullException("SpreadsheetId not configured");

        var credentialsPath = configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";
        GoogleCredential credential;
        
        #pragma warning disable CS0618 // Type or member is obsolete
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }
        #pragma warning restore CS0618 // Type or member is obsolete

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CT600 Submission"
        });
    }

    public async Task<List<CT600Data>> GetAllDataAsync()
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, Range);
            var response = await request.ExecuteAsync();

            if (response?.Values == null || !response.Values.Any())
            {
                return new List<CT600Data>();
            }

            return response.Values.Select(row => new CT600Data
            {
                CompanyName = row.Count > 0 ? row[0]?.ToString() ?? string.Empty : string.Empty,
                CompanyRegistrationNumber = row.Count > 1 ? row[1]?.ToString() ?? string.Empty : string.Empty,
                TaxReference = row.Count > 2 ? row[2]?.ToString() ?? string.Empty : string.Empty,
                PeriodStart = row.Count > 3 && DateTime.TryParse(row[3]?.ToString(), out var start) ? start : DateTime.MinValue,
                PeriodEnd = row.Count > 4 && DateTime.TryParse(row[4]?.ToString(), out var end) ? end : DateTime.MinValue,
                Turnover = row.Count > 5 && decimal.TryParse(row[5]?.ToString(), out var turnover) ? turnover : 0,
                TaxableProfit = row.Count > 6 && decimal.TryParse(row[6]?.ToString(), out var profit) ? profit : 0,
                TaxDue = row.Count > 7 && decimal.TryParse(row[7]?.ToString(), out var tax) ? tax : 0,
                Status = row.Count > 8 ? row[8]?.ToString() ?? "Draft" : "Draft",
                SubmissionReference = row.Count > 9 ? row[9]?.ToString() : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from Google Sheets");
            throw;
        }
    }

    public async Task<CT600Data?> GetDataByReferenceAsync(string taxReference)
    {
        var allData = await GetAllDataAsync();
        return allData.FirstOrDefault(d => d.TaxReference == taxReference);
    }

    public async Task<bool> UpdateDataAsync(string taxReference, CT600Data data)
    {
        try
        {
            var allData = await GetAllDataAsync();
            var rowIndex = allData.FindIndex(d => d.TaxReference == taxReference);

            if (rowIndex < 0)
            {
                return false;
            }

            var rowNumber = rowIndex + 2; // +2 because sheets are 1-indexed and we skip the header
            var updateRange = $"CT600Data!A{rowNumber}:J{rowNumber}";

            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        data.CompanyName,
                        data.CompanyRegistrationNumber,
                        data.TaxReference,
                        data.PeriodStart.ToString("yyyy-MM-dd"),
                        data.PeriodEnd.ToString("yyyy-MM-dd"),
                        data.Turnover,
                        data.TaxableProfit,
                        data.TaxDue,
                        data.Status,
                        data.SubmissionReference ?? string.Empty
                    }
                }
            };

            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await updateRequest.ExecuteAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data in Google Sheets");
            throw;
        }
    }

    public async Task<bool> AddDataAsync(CT600Data data)
    {
        try
        {
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        data.CompanyName,
                        data.CompanyRegistrationNumber,
                        data.TaxReference,
                        data.PeriodStart.ToString("yyyy-MM-dd"),
                        data.PeriodEnd.ToString("yyyy-MM-dd"),
                        data.Turnover,
                        data.TaxableProfit,
                        data.TaxDue,
                        data.Status,
                        data.SubmissionReference ?? string.Empty
                    }
                }
            };

            var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, Range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await appendRequest.ExecuteAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding data to Google Sheets");
            throw;
        }
    }
}
