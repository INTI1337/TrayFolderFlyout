using System.Collections.Generic;

namespace TrayFolderFlyout.Models
{
    public class AppConfig
    {
        public string Theme { get; set; } = "System"; // "System", "Dark", "Light"
        public int DefaultColumns { get; set; } = 4;
        public List<FolderGroup> Groups { get; set; } = new List<FolderGroup>();
    }
}
