using System;
using Nest;

namespace RegionalWeather.Elastic
{
    public class WeatherLocationDocument
    {
        public Guid Id { get; set; }
        public string LocationName { get; set; }
        public int LocationId { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime SunSet { get; set; }
        public DateTime DateTime { get; set; }
        public Location Location { get; set; }
        public Temperatures Temperatures { get; set;}
        public Clouds Clouds { get; set; }
        public Wind Wind { get; set; }
        
        public Rain Rain { get; set; }
        
        public GeoLocation GeoLocation { get; set; }
         
    }
}