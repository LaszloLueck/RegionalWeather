using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
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

        public bool BulkWriteDocument<T>(IEnumerable<T> documents, string indexName) where T : WeatherLocationDocument
        {
            var result = _elasticClient.IndexMany(documents, indexName);
            return ProcessResponse(result);
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            await Log.InfoAsync("Check if index exists");
            var ret = await _elasticClient.Indices.ExistsAsync(indexName);
            return ProcessResponse(ret);
        }
        
        public bool IndexExists(string indexName)
        {
            Log.Info("Check if index exists.");
            return _elasticClient.Indices.Exists(indexName).Exists;
        }

        public async Task<bool> CreateIndexAsync(string indexName)
        {
            await Log.InfoAsync($"Create index {indexName} with Mapping");
            var result = await _elasticClient
                .Indices
                .CreateAsync(indexName, index => index
                    .Map<WeatherLocationDocument>(x => x.AutoMap()
                        .Properties(d => d.Date(e => e.Name(en => en.TimeStamp)))));
            return ProcessResponse(result);
        }

        public bool CreateIndex(string indexName)
        {
            Log.Info($"Create index {indexName} with Mapping");
            var result = _elasticClient
                .Indices
                .Create(indexName, index => index
                    .Map<WeatherLocationDocument>(x => x.AutoMap()
                        .Properties(d => d.Date(e=>e.Name(en => en.TimeStamp)))));
            return ProcessResponse(result);
        }

        public bool DeleteIndex(string indexName)
        {
            Log.Info($"Delete index {indexName}");
            var result = _elasticClient
                .Indices.Delete(indexName);

            return ProcessResponse(result);
        }

        public async Task<bool> DeleteIndexAsync(string indexName)
        {
            await Log.InfoAsync($"Delete index {indexName}");
            var result = await _elasticClient
                .Indices
                .DeleteAsync(indexName);
            return ProcessResponse(result);
        }

        private bool ProcessResponse<T>(T response)
        {
            
            switch (response)
            {
                case ExistsResponse existsResponse:
                    if (existsResponse.IsValid) return existsResponse.Exists;
                    Log.Warning(existsResponse.DebugInformation);
                    Log.Error(existsResponse.OriginalException, existsResponse.ServerError.Error.Reason);
                    return existsResponse.Exists;
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
                case BulkResponse bulkResponse:
                    if (bulkResponse.IsValid)
                    {
                        Log.Info($"Successfully write {bulkResponse.Items.Count} documents to elastic");
                        return bulkResponse.IsValid;
                    }
                    Log.Warning(bulkResponse.DebugInformation);
                    Log.Error(bulkResponse.OriginalException, bulkResponse.ServerError.Error.Reason);
                    return bulkResponse.IsValid;
                default:
                    Log.Warning($"Cannot find Conversion for type <{response.GetType().Name}>");
                    return false;
            }
        }
        
    }
}