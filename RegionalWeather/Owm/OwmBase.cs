using System;
using System.Text.Json.Serialization;

namespace RegionalWeather.Owm
{
    public class OwmBase
    {
        [JsonPropertyName("guid")]
        public Guid Guid { get; set; } 
    }
}