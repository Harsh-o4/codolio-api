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
            string profileUrl = $"https://codolio.com/profile/{username}";

            return await PerformLiveScraping(username,profileUrl);
        }

        public async Task<ApiResponse<Profile>> GetProfileByUrl(string profileUrl)
        {
            if(string.IsNullOrWhiteSpace(profileUrl) || !Uri.TryCreate(profileUrl,UriKind.Absolute, out var uri))
            {
                return ApiResponse<Profile>.Fail("Invalid Profile URL.");
            }   

            string username = uri.Segments.LastOrDefault()?.Trim('/') ?? "unknown";
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
            try
            {
                _logger.LogInformation("Scraping information for user: {Username}",username);

                var profile = new Profile
                {
                    Username = username,
                    ProfileUrl = profileUrl,
                    ScrapedAt = DateTime.UtcNow
                };

                //Fetch HTML page
                string? pageHtml = null;
                try
                {
                    var htmlResponse = await _httpClient.GetAsync(profileUrl);
                    if (htmlResponse.IsSuccessStatusCode)
                    {
                        pageHtml = await htmlResponse.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e,"Error in fetching HTML page for {Usernam}",username);   
                }

                if (!string.IsNullOrEmpty(pageHtml))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(pageHtml);
                    ParseMetaTagsAndDom(doc,profile);
                }

                //Query Codolio's User Profile Microservice (userkey = username)
                string apiUrl = $"https://api.codolio.com/profile?userKey={Uri.EscapeDataString(username)}";
                try
                {
                    var apiResponse =  await _httpClient.GetAsync(apiUrl);
                    if (apiResponse.IsSuccessStatusCode)
                    {
                        string apiJson = await apiResponse.Content.ReadAsStringAsync();
                        ParseProfileJson(apiJson,profile);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Unable to fetech profile microservice payload for {Username}",username);
                }


                //Query Github stats
                string githubApiUrl = $"https://api.codolio.com/github/profile?userKey={Uri.EscapeDataString(username)}";
                try
                {
                    var ghResponse = await _httpClient.GetAsync(githubApiUrl);
                    if (ghResponse.IsSuccessStatusCode)
                    {
                        var ghJson = await ghResponse.Content.ReadAsStringAsync();
                        ParseGithubJson(ghJson,profile);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Unable to fetch github stats for {Username}",username);
                }

                //check if profile is invalid or private
                if(string.IsNullOrEmpty(profile.DisplayName) && profile.TotalProblemsSolved==0 && profile.Platforms.Count == 0)
                {
                    return ApiResponse<Profile>.Fail($"Codolio profile for '{username}' was not found or is private.");
                }

                return ApiResponse<Profile>.Ok(profile,$"Scraped profile data successfully.");
            } 
            catch (Exception e)
            {
                _logger.LogError(e,"Unexpected error while fetching profile for {Username}",username);
                return ApiResponse<Profile>.Fail($"Error while Scraping: {e.Message}");
            }
        }

        private void ParseMetaTagsAndDom(HtmlDocument doc, Profile profile)
        {
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "");
            var ogDesc = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", "");
            var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", "");
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;

            if (!string.IsNullOrEmpty(ogTitle)) profile.DisplayName = ogTitle.Split('|')[0].Trim();
            else if (!string.IsNullOrEmpty(title)) profile.DisplayName = title.Split('|')[0].Trim();

            if (!string.IsNullOrEmpty(ogDesc)) profile.Bio = ogDesc.Trim();
            if (!string.IsNullOrEmpty(ogImage)) profile.AvatarUrl = ogImage;

            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                foreach (var a in links)
                {
                    string href = a.GetAttributeValue("href", "");
                    if (href.Contains("github.com/")) profile.Links.Github = href;
                    else if (href.Contains("linkedin.com/")) profile.Links.Linkedin = href;
                    else if (href.Contains("twitter.com/") || href.Contains("x.com/")) profile.Links.Twitter = href;
                }
            }
        }


        private void ParseProfileJson(string json, Profile profile)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return;

                // Display Name & Profile Handle
                string first = data.TryGetProperty("firstName", out var fn) && fn.ValueKind == JsonValueKind.String ? fn.GetString() ?? "" : "";
                string second = data.TryGetProperty("secondName", out var sn) && sn.ValueKind == JsonValueKind.String ? sn.GetString() ?? "" : "";
                string full = $"{first} {second}".Trim();
                if (!string.IsNullOrEmpty(full)) profile.DisplayName = full;

                if (data.TryGetProperty("profileName", out var pn) && pn.ValueKind == JsonValueKind.String)
                {
                    profile.Username = pn.GetString() ?? profile.Username;
                }

                if (data.TryGetProperty("imageUrl", out var img) && img.ValueKind == JsonValueKind.String)
                {
                    profile.AvatarUrl = img.GetString();
                }

                // Parse userDetails (Bio, Country, College, Social Links)
                if (!data.TryGetProperty("userDetails", out var userDetails) || userDetails.ValueKind != JsonValueKind.Object)
                {
                }
                else
                {
                    if (userDetails.TryGetProperty("userPersonalDetails", out var upd) && upd.ValueKind == JsonValueKind.Object)
                    {
                        if (upd.TryGetProperty("bio", out var bio) && bio.ValueKind == JsonValueKind.String)
                            profile.Bio = bio.GetString();

                        if (upd.TryGetProperty("country", out var country) && country.ValueKind == JsonValueKind.String)
                            profile.Location = country.GetString();

                        if (upd.TryGetProperty("college", out var college) && college.ValueKind == JsonValueKind.String)
                            profile.Org = college.GetString();
                    }

                    if (userDetails.TryGetProperty("socialMediaProfileList", out var socialsList) && socialsList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var soc in socialsList.EnumerateArray())
                        {
                            if (soc.TryGetProperty("socialMediaPlatform", out var plat) && soc.TryGetProperty("handle", out var handle))
                            {
                                string pName = plat.GetString() ?? "";
                                string hVal = handle.GetString() ?? "";
                                if (string.IsNullOrWhiteSpace(hVal)) continue;

                                if (pName.Equals("linkedIn", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Linkedin = hVal.StartsWith("http") ? hVal : $"https://linkedin.com/in/{hVal.Trim('/')}";
                                else if (pName.Equals("gitHub", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Github = hVal.StartsWith("http") ? hVal : $"https://github.com/{hVal.Trim('/')}";
                                else if (pName.Equals("twitter", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Twitter = hVal.StartsWith("http") ? hVal : $"https://x.com/{hVal.Trim('/')}";
                                else if (pName.Equals("website", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Porfolio = hVal.StartsWith("http") ? hVal : $"https://{hVal}";
                            }
                        }
                    }
                }

                // Parse Connected Platform Stats (platformProfiles.platformProfiles)
                JsonElement platformArray = default;
                if (data.TryGetProperty("platformProfiles", out var ppObj) && ppObj.ValueKind == JsonValueKind.Object &&
                    ppObj.TryGetProperty("platformProfiles", out var ppList) && ppList.ValueKind == JsonValueKind.Array)
                {
                    platformArray = ppList;
                }
                else if (data.TryGetProperty("userPlatformProfileList", out var uppl) && uppl.ValueKind == JsonValueKind.Array)
                {
                    platformArray = uppl;
                }

                if (platformArray.ValueKind == JsonValueKind.Array)
                {
                    int combinedSolvedCount = 0;
                    profile.Platforms.Clear();

                    foreach (var item in platformArray.EnumerateArray())
                    {
                        var platform = new PlatformStats();

                        if (item.TryGetProperty("platform", out var pName) && pName.ValueKind == JsonValueKind.String)
                        {
                            platform.PlatformName = pName.GetString() ?? "";
                        }

                        // User stats (rating, rank, handle)
                        if (item.TryGetProperty("userStats", out var userStats) && userStats.ValueKind == JsonValueKind.Object)
                        {
                            if (userStats.TryGetProperty("handle", out var handle) && handle.ValueKind == JsonValueKind.String)
                            {
                                platform.Username = handle.GetString();
                                
                                if (platform.PlatformName.Equals("leetcode", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.LeetCode = $"https://leetcode.com/{platform.Username}";
                                else if (platform.PlatformName.Equals("codeforces", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Codeforces = $"https://codeforces.com/profile/{platform.Username}";
                                else if (platform.PlatformName.Equals("codechef", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.CodeChef = $"https://www.codechef.com/users/{platform.Username}";
                                else if (platform.PlatformName.Equals("geeksforgeeks", StringComparison.OrdinalIgnoreCase))
                                    profile.Links.Gfg = $"https://www.geeksforgeeks.org/user/{platform.Username}";
                            }

                            if (userStats.TryGetProperty("currentRating", out var cr) && cr.ValueKind == JsonValueKind.Number)
                                platform.CurrentRating = cr.GetDouble();

                            if (userStats.TryGetProperty("maxRating", out var mr) && mr.ValueKind == JsonValueKind.Number)
                                platform.MaxRating = mr.GetDouble();

                            if (userStats.TryGetProperty("rank", out var rank) && rank.ValueKind == JsonValueKind.String)
                                platform.RankTitle = rank.GetString();

                            if (userStats.TryGetProperty("university", out var univ) && univ.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(profile.Org))
                                profile.Org = univ.GetString();
                        }

                        // Solved question breakdown
                        if (item.TryGetProperty("totalQuestionStats", out var totalQuestionStats) && totalQuestionStats.ValueKind == JsonValueKind.Object)
                        {
                            if (totalQuestionStats.TryGetProperty("totalQuestionCounts", out var tqc) && tqc.ValueKind == JsonValueKind.Number)
                            {
                                platform.Solved = tqc.GetInt32();
                                combinedSolvedCount += platform.Solved;
                            }

                            if (totalQuestionStats.TryGetProperty("easyQuestionCounts", out var eqc) && eqc.ValueKind == JsonValueKind.Number)
                                platform.EasySolved = eqc.GetInt32();

                            if (totalQuestionStats.TryGetProperty("mediumQuestionCounts", out var mqc) && mqc.ValueKind == JsonValueKind.Number)
                                platform.MediumSolved = mqc.GetInt32();

                            if (totalQuestionStats.TryGetProperty("hardQuestionCounts", out var hqc) && hqc.ValueKind == JsonValueKind.Number)
                                platform.HardSolved = hqc.GetInt32();
                        }

                        profile.Platforms.Add(platform);
                    }

                    profile.TotalProblemsSolved = combinedSolvedCount;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,"Failed to parse Codolio profile JSON response for {Username}",profile.Username);
            }
        }
        private void ParseGithubJson(string json, Profile profile)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return;

                if (data.TryGetProperty("totalActiveDays", out var activeDays) && activeDays.ValueKind == JsonValueKind.Number)
                {
                    profile.ActiveStreak = activeDays.GetInt32();
                }

                if (data.TryGetProperty("commitCounts", out var commits) && commits.ValueKind == JsonValueKind.Number)
                {
                    profile.MaxStreak = commits.GetInt32();
                }

                if (data.TryGetProperty("githubProfile", out var ghHandle) && ghHandle.ValueKind == JsonValueKind.String)
                {
                    string handle = ghHandle.GetString()!;
                    if (string.IsNullOrEmpty(profile.Links.Github))
                    {
                        profile.Links.Github = $"https://github.com/{handle}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse GitHub JSON payload for {Username}", profile.Username);
            }
        }
    }
}

