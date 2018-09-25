namespace SampleApp1.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using SampleApp1.Types;
    using System;
    
    public class Competition: Document
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }

        [JsonProperty(PropertyName = "platforms")]
        public GamingPlatform[] Platforms { get; set; }

        [JsonProperty(PropertyName = "games")]
        public string[] Games { get; set; }

        [JsonProperty(PropertyName = "numberOfRegisteredCompetitors")]
        public int NumberOfRegisteredCompetitors { get; set; }

        [JsonProperty(PropertyName = "numberOfCompetitors")]
        public int NumberOfCompetitors { get; set; }

        [JsonProperty(PropertyName = "numberOfViewers")]
        public int NumberOfViewers { get; set; }

        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public CompetitionStatus Status { get; set; }

        [JsonProperty(PropertyName = "dateTime")]
        public DateTime DateTime { get; set; }

        [JsonProperty(PropertyName = "winners")]
        public Winner[] Winners { get; set; }
    }
}
