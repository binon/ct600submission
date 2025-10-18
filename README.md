# CT600 Submission System

A .NET 8 Web API application that integrates with HMRC's CT600 API to manage and submit Corporation Tax returns. The system uses Google Sheets as a data store, allowing users to view, edit, and submit CT600 data through a RESTful API.

## Overview

This application simplifies CT600 submission for small limited companies by providing:

- **Google Sheets Integration** - Uses Google Sheets as the backend data store for CT600 data
- **.NET 8 Web API** - RESTful API for authentication with HMRC, fetching, submitting, and monitoring CT600 data
- **HMRC Developer Sandbox** - Integration with HMRC's sandbox environment for testing before live use

## Features

- View all CT600 submissions from Google Sheets
- View, edit, and add individual CT600 records
- OAuth2 authentication with HMRC
- Submit CT600 data to HMRC
- Check submission status directly with HMRC
- Swagger/OpenAPI documentation

## Prerequisites

- .NET 8 SDK or later
- Google Cloud Platform account with Sheets API enabled
- HMRC Developer account with OAuth2 credentials

## Setup

### 1. Google Sheets Setup

1. Create a Google Cloud Project and enable the Google Sheets API
2. Create a service account and download the credentials JSON file
3. Create a Google Sheet with the following columns in a sheet named "CT600Data":
   - Company Name
   - Company Registration Number
   - Tax Reference
   - Period Start
   - Period End
   - Turnover
   - Taxable Profit
   - Tax Due
   - Status
   - Submission Reference

4. Share the Google Sheet with the service account email
5. Copy the Spreadsheet ID from the URL

### 2. HMRC Developer Setup

1. Register at [HMRC Developer Hub](https://developer.service.hmrc.gov.uk/)
2. Create an application to get Client ID and Client Secret
3. Configure the OAuth redirect URI (default: `http://localhost:5000/api/auth/callback`)

### 3. Application Configuration

1. Place your Google service account credentials file as `credentials.json` in the project root
2. Update `appsettings.json` with your configuration:

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "CredentialsPath": "credentials.json"
  },
  "Hmrc": {
    "BaseUrl": "https://test-api.service.hmrc.gov.uk",
    "ClientId": "YOUR_HMRC_CLIENT_ID",
    "ClientSecret": "YOUR_HMRC_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5000/api/auth/callback"
  }
}
```

## Running the Application

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

## API Endpoints

### Authentication

- `GET /api/auth/authorize` - Get HMRC authorization URL
- `GET /api/auth/callback?code={code}` - OAuth callback endpoint
- `GET /api/auth/status` - Check authentication status

### CT600 Data Management

- `GET /api/ct600` - Get all CT600 records
- `GET /api/ct600/{taxReference}` - Get specific CT600 record
- `POST /api/ct600` - Add new CT600 record
- `PUT /api/ct600/{taxReference}` - Update CT600 record
- `POST /api/ct600/{taxReference}/submit` - Submit CT600 to HMRC
- `GET /api/ct600/{taxReference}/status` - Get submission status from HMRC

## Usage Flow

1. **Authenticate with HMRC**
   ```bash
   # Get authorization URL
   curl http://localhost:5000/api/auth/authorize
   # Visit the URL and authorize
   # You'll be redirected to the callback URL
   ```

2. **View CT600 Data**
   ```bash
   # Get all records
   curl http://localhost:5000/api/ct600
   
   # Get specific record
   curl http://localhost:5000/api/ct600/{taxReference}
   ```

3. **Add/Update CT600 Data**
   ```bash
   # Add new record
   curl -X POST http://localhost:5000/api/ct600 \
     -H "Content-Type: application/json" \
     -d '{...}'
   
   # Update record
   curl -X PUT http://localhost:5000/api/ct600/{taxReference} \
     -H "Content-Type: application/json" \
     -d '{...}'
   ```

4. **Submit to HMRC**
   ```bash
   # Submit CT600
   curl -X POST http://localhost:5000/api/ct600/{taxReference}/submit
   
   # Check status
   curl http://localhost:5000/api/ct600/{taxReference}/status
   ```

## Project Structure

```
CT600Submission/
├── Controllers/
│   ├── AuthController.cs      # HMRC authentication endpoints
│   └── CT600Controller.cs     # CT600 data management endpoints
├── Models/
│   ├── CT600Data.cs           # CT600 data model
│   ├── HmrcAuthToken.cs       # Authentication token model
│   └── SubmissionStatus.cs    # Submission status model
├── Services/
│   ├── GoogleSheetsService.cs # Google Sheets integration
│   └── HmrcService.cs         # HMRC API integration
├── Program.cs                  # Application entry point
└── appsettings.json           # Configuration

```

## Security Considerations

- Never commit `credentials.json` or sensitive configuration to version control
- Use environment variables or Azure Key Vault for production secrets
- The HMRC sandbox environment should be used for testing
- Implement proper authentication and authorization for production use
- Consider implementing rate limiting and request throttling

## Development Notes

- Uses .NET 8 minimal API patterns
- Implements dependency injection for services
- Includes comprehensive error handling and logging
- Swagger/OpenAPI documentation for easy API exploration
- CORS enabled for development (should be configured properly for production)

## Testing with HMRC Sandbox

The application is configured to use HMRC's sandbox environment by default. This allows you to test submissions without affecting real tax data. Remember to:

1. Use test credentials from HMRC Developer Hub
2. Follow HMRC's test data scenarios
3. Switch to production URL when ready for live use

## License

This project is provided as-is for educational and development purposes.
