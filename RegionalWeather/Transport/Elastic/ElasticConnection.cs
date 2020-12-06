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
            
            if(result.Acknowledged) return result.Acknowledged;
            
            Log.Info(result.DebugInformation);
            Log.Info(result.ServerError.Error.ToString());
            Log.Info(result.OriginalException.Message);

            return result.Acknowledged;
            
        }
        
        
    }
    public abstract class Document
    {
        public JoinField Join { get; set; }
    }

    public class Location
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
    public class WeatherLocationDocument
    {
        public string LocationName { get; set; }
        public int LocationId { get; set; }
        public DateTime DateTime { get; set; }
        public Location Location { get; set; }
    }
}