using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.AirPollution
{
    public class AirPollutionBase : OwmBase
    {
        [JsonPropertyName("coord")] public Coord Coord { get; set; }

        [JsonPropertyName("list")] public List<List> List { get; set; }

        [JsonPropertyName("readTime")] public DateTime ReadTime { get; set; }

        [JsonPropertyName("name")] public string LocationName { get; set; }
    }
}