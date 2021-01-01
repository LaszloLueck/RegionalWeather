using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.CurrentWeather
{
    public class Clouds    {
        [JsonPropertyName("all")]
        public int All { get; set; } 
    }
}