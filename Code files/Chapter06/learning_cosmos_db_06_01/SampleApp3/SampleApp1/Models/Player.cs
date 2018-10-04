namespace SampleApp1.Models
{
    using Newtonsoft.Json;

    public class Player
    {
        [JsonProperty(PropertyName = "nickName")]
        public string NickName { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }
    }
}
