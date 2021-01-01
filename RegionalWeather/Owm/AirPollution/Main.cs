using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.AirPollution
{
    public class Main    {
        [JsonPropertyName("aqi")]
        public int Aqi { get; set; }
    }
}