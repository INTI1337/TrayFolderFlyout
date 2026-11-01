using System;
using System.Collections.Generic;

namespace TrayFolderFlyout.Models
{
    public class FolderGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public FolderType Type { get; set; } = FolderType.Virtual;
        public string? SyncPath { get; set; }
        public string? CustomIconPath { get; set; }
        public int Columns { get; set; } = 4;
        public bool ShowLabels { get; set; } = true;
        public List<FolderItem> Items { get; set; } = new List<FolderItem>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
