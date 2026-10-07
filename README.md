# Codolio Profile Scraper API

A lightweight ASP.NET Core Web API that retrieves and normalizes developer profile information from [Codolio](https://codolio.com). It combines profile details, social links, coding-platform statistics, GitHub activity, and project information into a consistent JSON response.

## Features

- Fetch a Codolio profile by username.
- Fetch a profile using its full Codolio URL.
- Retrieve the raw Codolio profile HTML for debugging or inspection.
- Collect profile metadata such as display name, bio, location, organization, and avatar.
- Collect coding-platform statistics, including solved problems, difficulty breakdowns, ratings, ranks, and handles.
- Include social links and project details when available.
- Provide interactive Swagger/OpenAPI documentation at the application root.
- Return a consistent response envelope containing `success`, `message`, `data`, and `timestamp`.

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

Some fields are optional and may be `null`, empty, or omitted from the upstream Codolio data.

### Profile fields

| Field | Description |
| --- | --- |
| `username` | Codolio profile handle. |
| `profileUrl` | Normalized Codolio profile URL. |
| `displayName` | Display name found on the profile. |
| `avatarUrl` | Profile image URL, when available. |
| `bio` | Profile biography. |
| `location` | Country or location information. |
| `org` | College, university, or organization. |
| `totalProblemsSolved` | Combined solved-problem count from available platforms. |
| `activeStreak` | GitHub activity value returned by the Codolio GitHub service. |
| `maxStreak` | GitHub activity value returned by the Codolio GitHub service. |
| `links` | Social and coding-platform profile links. |
| `platforms` | Detailed statistics for connected coding platforms. |
| `projects` | Projects listed on the Codolio profile. |
| `scrapedAt` | UTC time when the profile was collected. |

### Platform fields

Each item in `platforms` can contain:

- Platform name and username.
- Profile URL, when available.
- Total solved problems.
- Easy, medium, and hard solved counts.
- Current and maximum ratings.
- Rank title and star count, when available.

## How the API Works

The API performs live scraping and does not use a server-side cache.

1. **Receive the request**
   - The controller accepts either a username or a full Codolio profile URL.
   - Usernames are trimmed and an optional leading `@` is removed.

2. **Build and validate the Codolio URL**
   - Username requests are converted to `https://codolio.com/profile/{username}`.
   - URL requests must use HTTPS and the `codolio.com/profile/{username}` path format.

3. **Fetch the public profile page**
   - The service requests the Codolio HTML page with a browser-like `User-Agent`, `Referer`, and `Accept` headers.
   - HTML metadata such as the page title, description, avatar, and social links is extracted with Html Agility Pack.

4. **Fetch structured profile data**
   - The service requests Codolio's profile service using the username.
   - It parses profile information, social accounts, connected coding platforms, ratings, and solved-problem counts.

5. **Fetch GitHub statistics**
   - The service requests the Codolio GitHub profile service.
   - GitHub-related activity values and a GitHub profile link are added when available.

6. **Combine the results**
   - Data from the HTML page and upstream services is merged into the `Profile` model.
   - The API returns the normalized profile inside an `ApiResponse<Profile>` envelope.

7. **Handle unavailable profiles**
   - If no meaningful profile information is found, the API returns a failure response indicating that the profile was not found or may be private.

Because requests are live, response time and returned data depend on the availability and current structure of Codolio's public pages and services.

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

## Important Notes

- Profile data is fetched live for each request; there is currently no caching layer.
- The API depends on Codolio's public HTML and upstream service responses. Changes to those services or page structures may require parser updates.
- The configured HTTP client timeout is 25 seconds.
- CORS is currently configured to allow requests from any origin. Review this setting before deploying publicly.
- Use the API responsibly and respect Codolio's terms, robots policies, and service limits.

## License

No license has been specified for this repository yet. Add a license file if you plan to distribute or reuse the project publicly.
