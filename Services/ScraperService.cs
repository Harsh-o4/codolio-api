using System.Text.Json;
using System.Text.RegularExpressions;
using Codolio.Models;
using HtmlAgilityPack;
using Codolio.Services;
using Microsoft.AspNetCore.Identity;

namespace Codolio.Services
{
    public class ScraperService: IScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ScraperService> _logger;

        public ScraperService(HttpClient httpClient,ILogger<ScraperService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                _httpClient.DefaultRequestHeaders.Add("Accept","text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,application/json,*/*;q=0.8");
                _httpClient.DefaultRequestHeaders.Add("Accept-Language","en-US,en;q=0.9");
                _httpClient.DefaultRequestHeaders.Add("Origin","https://codolio.com/");
                _httpClient.DefaultRequestHeaders.Add("Referer","https://codolio.com/");
            }
        }

        public async Task<ApiResponse<Profile>> GetProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return ApiResponse<Profile>.Fail("Username cannot be empty.");
            }   

            username = username.Trim().TrimStart('@');
            string profileUrl = $"https://coodlio.com/profile/{username}";

            return await PerformLiveScraping(username,profileUrl);
        }

        public async Task<ApiResponse<Profile>> GetProfileByUrl(string profileUrl)
        {
            if(string.IsNullOrWhiteSpace(profileUrl) || !Uri.TryCreate(profileUrl,UriKind.Absolute, out var uri))
            {
                return ApiResponse<Profile>.Fail("Invalid Profile URL.");
            }   

            string username = uri.Segments.LastOrDefault()?.Trim()('/')??"unknown";
            return await PerformLiveScraping(username,profileUrl);
        }

        public async Task<string?> GetRawHtml(string username)
        {
            try
            {
                string url = $"https://codolio.com/profile/{username.Trim().TrimStart('@')}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e,"Error fetching raw HTML for {username}",username);
            }

            return null;
        }


        private async Task<ApiResponse<Profile>> PerformLiveScraping(string username, string profileUrl)
        {
            
        }
    }
}

