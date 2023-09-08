namespace SampleApp1.Cosmos.Types
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GamingPlatform
    {
        Switch,
        PC,
        PS4,
        XBox,
        iOS,
        Android
    }
}