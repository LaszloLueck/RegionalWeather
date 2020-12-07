using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Nest;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
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
            return ProcessResponse(result);
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
            return ProcessResponse(result);
        }

        public bool DeleteIndex(string indexName)
        {
            Log.Info($"Delete index {indexName}");
            var result = _elasticClient
                .Indices.Delete(indexName);

            return ProcessResponse(result);
        }

        private bool ProcessResponse<T>(T response)
        {
            
            switch (response)
            {
                case DeleteIndexResponse deleteIndexResponse :
                    if (deleteIndexResponse.Acknowledged) return deleteIndexResponse.Acknowledged;
                    Log.Warning(deleteIndexResponse.DebugInformation);
                    Log.Error(deleteIndexResponse.OriginalException, deleteIndexResponse.ServerError.Error.Reason);
                    return deleteIndexResponse.Acknowledged;
                case IndexResponse indexResponse:
                    if (indexResponse.IsValid) return indexResponse.IsValid;
                    Log.Warning(indexResponse.DebugInformation);
                    Log.Error(indexResponse.OriginalException, indexResponse.ServerError.Error.Reason);
                    return indexResponse.IsValid;
                case CreateIndexResponse createIndexResponse:
                    if (createIndexResponse.IsValid) return createIndexResponse.IsValid;
                    Log.Warning(createIndexResponse.DebugInformation);
                    Log.Error(createIndexResponse.OriginalException, createIndexResponse.ServerError.Error.Reason);
                    return createIndexResponse.IsValid;
            }
            
            return true;
        }
        
    }
}