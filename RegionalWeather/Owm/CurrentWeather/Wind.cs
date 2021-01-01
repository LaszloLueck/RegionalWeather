using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.CurrentWeather
{
    public class Wind    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; } 

        [JsonPropertyName("deg")]
        public int Deg { get; set; } 
        
        [JsonPropertyName("gust")]
        public double Gust { get; set; }
    }
}