using System;
using System.IO;

namespace TrayFolderFlyout.Models
{
    public class FolderItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public string TargetPath { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string? CustomIconPath { get; set; }
        public bool RunAsAdmin { get; set; } = false;

        public bool IsDirectory => !string.IsNullOrEmpty(TargetPath) && Directory.Exists(TargetPath);

        public bool IsUrl => !string.IsNullOrEmpty(TargetPath) &&
            (TargetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             TargetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
             TargetPath.EndsWith(".url", StringComparison.OrdinalIgnoreCase));
    }
}
