using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Nest;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;

namespace RegionalWeather.Transport.Elastic
{
    public interface IElasticConnectionBuilder
    {
        ElasticConnection Build(ConfigurationItems configurationItems);
    }

    public class ElasticConnectionBuilder : IElasticConnectionBuilder
    {
        public ElasticConnection Build(ConfigurationItems configurationItems)
        {
            return new(configurationItems);
        }
    }

    public class ElasticConnection
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ElasticConnection>.GetLogger();
        private readonly ElasticClient _elasticClient;

        private static IEnumerable<Uri> BuildServerList(string hostsAndPorts)
        {
            return hostsAndPorts.Split(",").Select(uri => new Uri(uri));
        }

        public ElasticConnection(ConfigurationItems configurationItems)
        {
            var pool = new StaticConnectionPool(BuildServerList(configurationItems.ElasticHostsAndPorts).ToArray());
            var setting = new ConnectionSettings(pool);
            _elasticClient = new ElasticClient(setting);
        }

        public bool WriteDocument<T>(T document, string indexName) where T : WeatherLocationDocument
        {
            Log.Info($"Write Document {document.LocationId}");
            var result = _elasticClient.Index(document, i => i.Index(indexName));
            if (result.IsValid) return result.IsValid;
            Log.Info(result.DebugInformation);
            Log.Info(result.ServerError.Error.ToString());
            Log.Info(result.OriginalException.Message);

            return result.IsValid;
        }

        public bool IndexExists(string indexName)
        {
            Log.Info("Check if index exists.");
            return _elasticClient.Indices.Exists(indexName).Exists;
        }

        public bool CreateIndex(string indexName)
        {
            Log.Info($"Create index {indexName} with Mapping");
            var result = _elasticClient
                .Indices
                .Create(indexName, index => index.Map<WeatherLocationDocument>(x => x.AutoMap()));
            if (result.Acknowledged) return result.Acknowledged;
            Log.Info(result.DebugInformation);
            Log.Info(result.ServerError.Error.ToString());
            Log.Info(result.OriginalException.Message);

            return result.Acknowledged;
        }

        public bool DeleteIndex(string indexName)
        {
            Log.Info($"Delete index {indexName}");
            var result = _elasticClient
                .Indices.Delete(indexName);

            if (result.Acknowledged) return result.Acknowledged;

            Log.Info(result.DebugInformation);
            Log.Info(result.ServerError.Error.ToString());
            Log.Info(result.OriginalException.Message);

            return result.Acknowledged;
        }
    }


    public class Temperatures
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TemperatureMin { get; set; }
        public double TemperatureMax { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
    }

    public class Location
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
    
    public class Clouds {
        public string CloudType { get; set; }
        public string Description { get; set; }
        public int Visibility { get; set; }
        public int Density { get; set; }
    }

    public class Wind
    {
        public double Speed { get; set; }
        public int Direction { get; set; }
    }

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