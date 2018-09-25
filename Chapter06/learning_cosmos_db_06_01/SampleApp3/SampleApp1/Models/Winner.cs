namespace SampleApp1.Models
{
    using Newtonsoft.Json;

    public class Winner
    {
        [JsonProperty(PropertyName = "player")]
        public Player Player { get; set; }

        [JsonProperty(PropertyName = "position")]
        public int Position { get; set; }

        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }

        [JsonProperty(PropertyName = "prize")]
        public int Prize { get; set; }
    }
}
