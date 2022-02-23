using System;

namespace RevitToIfcScheduler.Models
{
    public class File: Base
    {
        public string Type { get; } = "file";
        public string ItemId { get; set; }
        public string FileType { get; set; } = null;
        public string FolderId { get; set; } = null;
        public Boolean IsCompositeDesign { get; set; }
    }
}