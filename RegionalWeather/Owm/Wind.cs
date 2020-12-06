using System.Text.Json.Serialization;

namespace RegionalWeather.Owm
{
    public class Wind    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; } 

        [JsonPropertyName("deg")]
        public int Deg { get; set; } 
    }
}