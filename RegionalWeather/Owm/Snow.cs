using System.Text.Json.Serialization;

namespace RegionalWeather.Owm
{
    public class Snow
    {
        [JsonPropertyName("1h")]
        public double OneHour { get; set; } 

        [JsonPropertyName("3h")]
        public double ThreeHour { get; set; } 
    }
}