# Codolio Profile Scraper API

ASP.NET Core API that retrieves a Codolio profile and its connected platform statistics.

## Run

```bash
dotnet run
```

Swagger is available at the application root. The default development URL is
`http://localhost:5004`.

## Endpoints

- `GET /api/Profile/profile/{username}`
- `GET /api/Profile/profile-by-url?url=https%3A%2F%2Fcodolio.com%2Fprofile%2F{username}`
- `GET /api/Profile/raw-html/{username}`

The profile endpoints return an envelope containing `success`, `message`, `data`,
and `timestamp`. Requests to `profile-by-url` are restricted to HTTPS Codolio
profile URLs.
