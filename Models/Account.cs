using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace RevitToIfcScheduler.Models
{
    public class Account
    {
        [Key]
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("region")]
        public string Region { get; set; }

        [NotMapped]
        [JsonProperty("accountId")]
        public string AccountId
        {
            get { return Id.Substring(2); }
        }
        [NotMapped]
        [JsonProperty("hubId")]
        public string HubId
        {
            get { return Id; }
        }
    }
}