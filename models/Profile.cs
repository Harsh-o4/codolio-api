namespace Codolio.Models
{
    public class Profile
    {
       public string Username {get; set;} = string.Empty; 
       public string ProfileUrl {get; set;} = string.Empty; 
       public string DisplayName {get; set;} = string.Empty; 
       public string? AvatarUrl {get; set;}
       public string? CoverUrl {get; set;}
       public string? Bio {get; set;}
       public string? Location {get; set;}
       public string? Org {get; set;}

       //Codolio Metrics
       public int? CScore {get; set;}
       public int? GlobalScore {get; set;}
       public int TotalProblemsSolved {get; set;}
       public int ActiveStreak {get; set;}
       public int MaxStreak {get; set;}


       //Detailed Sections
       public Socials Links {get; set;} = new Socials();
       public List<PlatformStats> Platforms {get; set;} = new List<PlatformStats>();
       public List<ProjectDetail> Projects {get; set;} = new List<ProjectDetail>();

       //Scraping Metadata
       public DateTime ScrapedAt {get; set;} = DateTime.UtcNow;
    }

    public class Socials
    {
        public string? Github {get; set;}
        public string? Linkedin {get; set;}
        public string? Twitter {get; set;}
        public string? Porfolio {get; set;}
        public string? LeetCode {get; set;}
        public string? Codeforces {get; set;}
        public string? CodeChef {get; set;}
        public string? Gfg {get; set;}
    }

    public class PlatformStats
    {
        public string PlatformName {get; set;} = string.Empty;
        public string? Username {get; set;}
        public string? ProfileUrl {get; set;}
        public int Solved {get; set;}
        public int EasySolved {get; set;}
        public int MediumSolved {get; set;}
        public int HardSolved {get; set;}
        public double? CurrentRating {get; set;}
        public double? MaxRating {get; set;}
        public string? RankTitle {get; set;}
        public int? Stars {get; set;}
   
    }

    public class ProjectDetail
    {
       public string Title {get; set;} = string.Empty;
       public string? Description {get; set;}
       public List<string> TechStack {get; set;} = new List<string>();
       public string? GithubUrl {get; set;}
       public string? LiveUrl {get; set;}
       public string? PreviewImageUrl {get; set;}
    }

    public class ApiResponse<T>
    {
        public bool Success {get; set;}
        public string Message {get; set;} = string.Empty;
        public T? Data {get;  set;}
        public DateTime Timestamp {get; set;} = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = default
            };
        }

        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }    
    } 
}