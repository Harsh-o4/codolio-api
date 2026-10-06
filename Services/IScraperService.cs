using Codolio.Models;

namespace Codolio.Services
{
    public interface IScraperService
    {
        Task<ApiResponse<Profile>> GetProfile(string usernameCoded);
        Task<ApiResponse<Profile>> GetProfileByUrl(string profileUrl);
        Task<string?> GetRawHtml(string username);
    }
}