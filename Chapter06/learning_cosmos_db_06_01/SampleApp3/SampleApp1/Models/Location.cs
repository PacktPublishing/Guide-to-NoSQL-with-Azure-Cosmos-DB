namespace SampleApp1.Models
{
    using Newtonsoft.Json;

    public class Location
    {
        [JsonProperty(PropertyName = "zipCode")]
        public string ZipCode { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }
}
