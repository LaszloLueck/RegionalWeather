using System;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Elastic
{
    public class WeatherLocationDocument
    {
        public string LocationName { get; set; }
        public int LocationId { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime SunSet { get; set; }
        public DateTime DateTime { get; set; }
        public Location Location { get; set; }
        public Temperatures Temperatures { get; set;}
        public Clouds Clouds { get; set; }
        public Wind Wind { get; set; }
        
    }
}