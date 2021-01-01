using System;
using Nest;

namespace RegionalWeather.Elastic
{
    public class ElasticDocument
    {
        public Guid Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime DateTime { get; set; }
        public string LocationName { get; set; }
        public GeoLocation GeoLocation { get; set; }
    }

    public class AirPollutionDocument : ElasticDocument
    {
        public int AirQualityIndex { get; set; }
        public double No { get; set; }
        public double Co { get; set; }
        public double No2 { get; set; }
        public double O3 { get; set; }
        public double So2 { get; set; }
        public double Pm25 { get; set; }
        public double Pm10 { get; set; }
        public double Nh3 { get; set; }
    }

    public class WeatherLocationDocument : ElasticDocument
    {
        public int LocationId { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime SunSet { get; set; }
        public Location Location { get; set; }
        public Temperatures Temperatures { get; set;}
        public Clouds Clouds { get; set; }
        public Wind Wind { get; set; }
        public Rain Rain { get; set; }
        public Snow Snow { get; set; }
    }
}