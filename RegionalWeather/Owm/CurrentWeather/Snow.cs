﻿using System;
using System.Text.Json.Serialization;

namespace RegionalWeather.Owm.CurrentWeather
{
    public class Snow
    {
        [JsonPropertyName("1h")]
        public Double OneHour { get; set; }
        
        [JsonPropertyName("3h")]
        public Double ThreeHour { get; set; }
    }
}