using System.Collections.Generic;

namespace HospitalWebApp.Models
{
    public class AboutViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string IntroText { get; set; } = string.Empty;
        public IList<BlogPost> RecentBlogPosts { get; set; } = new List<BlogPost>();
    }
}
