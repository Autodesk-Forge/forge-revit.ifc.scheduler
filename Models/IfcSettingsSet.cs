using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace RevitToIfcScheduler.Models
{
    public class IfcSettingsSet
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
    }
}