using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitToIfcScheduler.Models
{
    public class ConversionBatch
    {
        [JsonProperty("files")]
        public List<File> Files { get; set; }
        [JsonProperty("folderUrns")]
        public List<string> FolderUrns { get; set; }
        
        [JsonProperty("ifcSettingsName")]
        public string ifcSettingsName { get; set; }
    }
}