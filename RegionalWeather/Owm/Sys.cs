using System.Text.Json.Serialization;

namespace RegionalWeather.Owm
{
    public class Sys    {
        [JsonPropertyName("type")]
        public int Type { get; set; } 

        [JsonPropertyName("id")]
        public int Id { get; set; } 

        [JsonPropertyName("message")]
        public double Message { get; set; } 

        [JsonPropertyName("country")]
        public string Country { get; set; } 

        [JsonPropertyName("sunrise")]
        public int Sunrise { get; set; } 

        [JsonPropertyName("sunset")]
        public int Sunset { get; set; } 
    }
}