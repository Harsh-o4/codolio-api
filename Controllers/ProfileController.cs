using Codolio.Models;
using Codolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codolio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]

    public class ProfileController : ControllerBase
    {
        private readonly IScraperService _scraperService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IScraperService scraperService,
            ILogger<ProfileController> logger
        )
        {
            _scraperService = scraperService;
            _logger = logger;
        }

        /// <summary>
        /// Live-scrapes a Codolio developer profile by username or handle in real-time.
        /// No caching is performed; every call fetches the latest HTML directly from Codolio.
        /// </summary>
        /// <param name="username">Codolio username handle (e.g. 'H', 'harsh', '@H')</param>
        [HttpGet("profile/{username}")]
        [ProducesResponseType(typeof(ApiResponse<Profile>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Profile>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<Profile>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(ApiResponse<Profile>.Fail("Username parameter is required."));
            }

            _logger.LogInformation("Real-time live scraping request received for user '{Username}'", username);
            var result = await _scraperService.GetProfile(username);

            if (!result.Success || result.Data == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Live-scrapes a Codolio profile directly from a full Codolio profile URL.
        /// </summary>
        /// <param name="url">Full profile URL (e.g. 'https://codolio.com/profile/H')</param>
        [HttpGet("profile-by-url")]
        [ProducesResponseType(typeof(ApiResponse<Profile>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Profile>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfileByUrl([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(ApiResponse<Profile>.Fail("URL query parameter is required."));
            }

            var result = await _scraperService.GetProfileByUrl(url);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the live, un-cached raw HTML string of a Codolio profile page.
        /// </summary>
        /// <param name="username">Codolio username handle</param>
        [HttpGet("raw-html/{username}")]
        [Produces("text/html")]
        public async Task<IActionResult> GetRawHtml(string username)
        {
            var html = await _scraperService.GetRawHtml(username);
            if (string.IsNullOrEmpty(html))
            {
                return NotFound("Could not retrieve HTML page.");
            }

            return Content(html, "text/html");
        }
    }
}