namespace RevitToIfcScheduler.Models
{
    public class Folder : Base
    {
        public string Type { get; } = "folder";
        public string WebView { get; set; } = null;
    }
}