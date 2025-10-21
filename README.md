# CT600 Submission System

A complete .NET 8 web application that integrates with HMRC's CT600 API to manage and submit Corporation Tax returns. The system includes both a user-friendly web UI and a RESTful API, using Google Sheets as a data store.

## Overview

This application simplifies CT600 submission for small limited companies by providing:

- **Web User Interface** - Modern responsive web UI for managing CT600 submissions
- **Google Sheets Integration** - Uses Google Sheets as the backend data store for CT600 data
- **.NET 8 Web API** - RESTful API for authentication with HMRC, fetching, submitting, and monitoring CT600 data
- **OpenAPI/Swagger Documentation** - Interactive API documentation for developers
- **HMRC Developer Sandbox** - Integration with HMRC's sandbox environment for testing before live use

## Features

### Web User Interface
- **Dashboard** - View all CT600 submissions with statistics (total, submitted, draft)
- **Detail View** - View and manage individual CT600 records
- **Authentication Page** - Easy HMRC OAuth2 authentication setup
- **Real-time Updates** - Dynamic data loading and status checks
- **Responsive Design** - Works on desktop and mobile devices

### API Features
- View all CT600 submissions from Google Sheets
- View, edit, and add individual CT600 records
- OAuth2 authentication with HMRC
- Submit CT600 data to HMRC
- Check submission status directly with HMRC
- Swagger/OpenAPI documentation for API exploration

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

The application will be available at:
- **Web UI**: `http://localhost:5000` (main dashboard)
- **API Docs**: `http://localhost:5000/swagger` (Swagger UI)
- **HTTPS**: `https://localhost:5001` (if configured)

### Using the Web Interface

1. **Dashboard** - Navigate to `http://localhost:5000`
   - View all CT600 submissions in a table
   - See statistics (total, submitted, draft counts)
   - Click "View" to see details of a specific record
   - Click "Submit" to submit a draft to HMRC
   - Click "Status" to check submission status

2. **Authentication** - Navigate to `http://localhost:5000/Home/Auth`
   - Check your HMRC authentication status
   - Start the OAuth2 authentication flow
   - Follow the authorization URL to authenticate with HMRC

3. **API Documentation** - Navigate to `http://localhost:5000/swagger`
   - Explore all available API endpoints
   - Test API calls directly from the browser

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

### Using the Web UI (Recommended for End Users)

1. **Start the Application**
   - Navigate to `http://localhost:5000`
   - You'll see the dashboard with CT600 submissions

2. **Authenticate with HMRC**
   - Click "Authentication" in the navigation bar
   - Click "Start Authentication" button
   - Copy and visit the authorization URL
   - Log in with your HMRC credentials and authorize
   - You'll be redirected back to the application

3. **View and Manage CT600 Data**
   - View all records on the dashboard
   - Click "View" on any record to see details
   - Click "Submit" to submit a draft to HMRC
   - Click "Status" to check submission status

### Using the API (For Developers)

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
│   ├── AuthController.cs      # HMRC authentication API endpoints
│   ├── CT600Controller.cs     # CT600 data management API endpoints
│   └── HomeController.cs      # Web UI MVC controller
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml       # Dashboard page
│   │   ├── Detail.cshtml      # CT600 detail/edit page
│   │   └── Auth.cshtml        # Authentication page
│   ├── Shared/
│   │   └── _Layout.cshtml     # Shared layout with navigation
│   ├── _ViewStart.cshtml      # View configuration
│   └── _ViewImports.cshtml    # Razor directives
├── Models/
│   ├── CT600Data.cs           # CT600 data model
│   ├── HmrcAuthToken.cs       # Authentication token model
│   └── SubmissionStatus.cs    # Submission status model
├── Services/
│   ├── GoogleSheetsService.cs # Google Sheets integration
│   └── HmrcService.cs         # HMRC API integration
├── wwwroot/                    # Static files (CSS, JS, images)
├── Program.cs                  # Application entry point
└── appsettings.json           # Configuration

```

## Security Considerations

- Never commit `credentials.json` or sensitive configuration to version control
- Use environment variables or Azure Key Vault for production secrets
- The HMRC sandbox environment should be used for testing
- Implement proper authentication and authorization for production use
- Consider implementing rate limiting and request throttling

## Architecture

### Web UI Layer
- **MVC Pattern** - HomeController serves Razor views
- **Responsive Design** - Bootstrap 5 for mobile-friendly interface
- **AJAX Integration** - jQuery-based API calls for dynamic updates
- **Real-time Status** - Authentication status displayed in navigation

### API Layer
- **RESTful Design** - Clean API endpoints following REST principles
- **OpenAPI/Swagger** - Interactive documentation at `/swagger`
- **CORS Support** - Environment-aware CORS configuration

### Backend Services
- **Dependency Injection** - Services registered and managed via DI container
- **Google Sheets Service** - Data persistence layer
- **HMRC Service** - External API integration with OAuth2

## Development Notes

- Uses .NET 8 with both MVC and API patterns
- Implements dependency injection for services
- Includes comprehensive error handling and logging
- Swagger/OpenAPI documentation for easy API exploration
- CORS enabled for development (should be configured properly for production)
- Static files support for web UI assets
- Razor Pages with shared layouts for consistent UI

## Testing with HMRC Sandbox

The application is configured to use HMRC's sandbox environment by default. This allows you to test submissions without affecting real tax data. Remember to:

1. Use test credentials from HMRC Developer Hub
2. Follow HMRC's test data scenarios
3. Switch to production URL when ready for live use

## License

This project is provided as-is for educational and development purposes.
