# Codolio Profile Scraper API

A lightweight ASP.NET Core Web API that retrieves and normalizes developer profile information from [Codolio](https://codolio.com). It combines profile details, social links, coding-platform statistics, GitHub activity, and project information into a consistent JSON response.

## Project Structure

```text
.
├── Controllers/
│   └── ProfileController.cs   # HTTP endpoints and request validation
├── Services/
│   ├── IScraperService.cs     # Scraper service contract
│   └── ScraperService.cs      # Codolio requests, parsing, and aggregation
├── models/
│   └── Profile.cs              # Profile, platform, project, and API response models
├── Program.cs                 # Dependency injection, CORS, Swagger, and app startup
├── appsettings.json           # Application configuration
├── test.http                  # Example HTTP requests
└── codolio_scraper.csproj     # .NET project and package references
```

## Features

- Fetch a Codolio profile by username.
- Fetch a profile using its full Codolio URL.
- Retrieve the raw Codolio profile HTML for debugging or inspection.
- Collect coding-platform statistics, including solved problems, difficulty breakdowns, ratings, ranks, and handles.
- Include social links and project details when available.

## Technology Stack

- C#
- ASP.NET Core Web API
- .NET 10
- `HttpClient` for upstream requests
- Html Agility Pack for HTML parsing
- Swashbuckle for Swagger/OpenAPI

## Getting Started

### Prerequisites

Install the .NET 10 SDK before running the project.

Verify your installation:

```bash
dotnet --version
```

### Clone the repository

```bash
git clone https://github.com/Harsh-o4/codolio-api.git
cd codolio-api
```

### Restore dependencies

```bash
dotnet restore
```

### Run the API

```bash
dotnet run
```

The API is configured to run locally at:

```text
http://localhost:5004
```

Swagger UI is available at the application root:

```text
http://localhost:5004/
```

> The exact local URL may vary depending on your .NET launch profile. Check the terminal output after running `dotnet run` if the API starts on a different port.

## How to Use the API

Set the API base URL first:

```bash
BASE_URL=http://localhost:5004
```

The examples below use `curl`, but you can call the API from any HTTP client, frontend application, Postman, or the Swagger UI.

### 1. Get a profile by username

```bash
curl "$BASE_URL/api/Profile/profile/harsh"
```

The username can also include an `@` prefix. The API removes the prefix before requesting the Codolio profile.

Example:

```http
GET /api/Profile/profile/harsh
Accept: application/json
```

### 2. Get a profile by full Codolio URL

```bash
curl --get "$BASE_URL/api/Profile/profile-by-url" \\
  --data-urlencode "url=https://codolio.com/profile/harsh"
```

Only HTTPS Codolio profile URLs are accepted. The expected format is:

```text
https://codolio.com/profile/{username}
```

Example:

```http
GET /api/Profile/profile-by-url?url=https%3A%2F%2Fcodolio.com%2Fprofile%2Fharsh
Accept: application/json
```

### 3. Get the raw profile HTML

Use this endpoint when you need to inspect the original HTML returned by Codolio or troubleshoot parsing issues.

```bash
curl "$BASE_URL/api/Profile/raw-html/harsh"
```

Example:

```http
GET /api/Profile/raw-html/harsh
Accept: text/html
```

This endpoint returns the upstream HTML directly instead of the normalized JSON profile object.

## Response Format

The profile endpoints return a response envelope similar to the following:

```json
{
  "success": true,
  "message": "Scraped profile data successfully.",
  "data": {
    "username": "harsh",
    "profileUrl": "https://codolio.com/profile/harsh",
    "displayName": "Example User",
    "avatarUrl": "https://example.com/avatar.png",
    "coverUrl": null,
    "bio": "Developer and competitive programmer",
    "location": "India",
    "org": "Example University",
    "cScore": null,
    "globalScore": null,
    "totalProblemsSolved": 250,
    "activeStreak": 42,
    "maxStreak": 18,
    "links": {
      "github": "https://github.com/example",
      "linkedin": "https://linkedin.com/in/example",
      "twitter": null,
      "porfolio": null,
      "leetCode": "https://leetcode.com/example",
      "codeforces": null,
      "codeChef": null,
      "gfg": null
    },
    "platforms": [
      {
        "platformName": "leetcode",
        "username": "example",
        "profileUrl": null,
        "solved": 250,
        "easySolved": 120,
        "mediumSolved": 105,
        "hardSolved": 25,
        "currentRating": 1750,
        "maxRating": 1850,
        "rankTitle": null,
        "stars": null
      }
    ],
    "projects": [],
    "scrapedAt": "2026-10-07T00:00:00Z"
  },
  "timestamp": "2026-10-07T00:00:00Z"
}
```

## HTTP Status Codes and Errors

### Successful response

- `200 OK` — Profile data or raw HTML was retrieved successfully.

### Client errors

- `400 Bad Request` — Missing username, missing URL, invalid URL, or a URL that is not a valid HTTPS Codolio profile URL.
- `404 Not Found` — The profile could not be found, is private, or raw HTML could not be retrieved.

Example error response:

```json
{
  "success": false,
  "message": "Codolio profile for 'unknown-user' was not found or is private.",
  "data": null,
  "timestamp": "2026-10-07T00:00:00Z"
}
```

## Swagger and API Testing

When the application is running, open:

```text
http://localhost:5004/
```

From Swagger UI, select an endpoint, enter a username or URL, and choose **Execute** to view the request and response.

The repository also includes sample requests in [`test.http`](./test.http), which can be run from editors such as Visual Studio or JetBrains Rider.

