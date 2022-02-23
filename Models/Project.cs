using Newtonsoft.Json;

namespace RevitToIfcScheduler.Models
{
    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("hubId")]
        public string HubId { get; set; }
    }
}