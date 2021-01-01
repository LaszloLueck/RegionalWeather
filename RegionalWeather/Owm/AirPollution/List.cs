using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.AirPollution
{
    public class List    {
        [JsonPropertyName("dt")]
        public int Dt { get; set; } 

        [JsonPropertyName("main")]
        public Main Main { get; set; } 

        [JsonPropertyName("components")]
        public Components Components { get; set; }
    }
}