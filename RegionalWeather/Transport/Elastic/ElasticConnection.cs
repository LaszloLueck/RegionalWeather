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
            return new ElasticConnection(configurationItems);
        }
    }

    public interface IElasticConnection
    {
        public Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : WeatherLocationDocument;
        public Task<bool> IndexExistsAsync(string indexName);
        public Task<bool> CreateIndexAsync(string indexName);
        public Task<bool> DeleteIndexAsync(string indexName);
    }

    public class ElasticConnection : IElasticConnection
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

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : WeatherLocationDocument
        {
            var result = await _elasticClient.IndexManyAsync(documents, indexName);
            await ProcessResponse(result);
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            await Log.InfoAsync("Check if index exists");
            var ret = await _elasticClient.Indices.ExistsAsync(indexName);
            return ret.Exists;
        }

        public async Task<bool> CreateIndexAsync(string indexName)
        {
            await Log.InfoAsync($"Create index {indexName} with Mapping");
            var result = await _elasticClient
                .Indices
                .CreateAsync(indexName, index => index
                    .Map<WeatherLocationDocument>(x => x.AutoMap()
                        .Properties(d => d.Date(e => e.Name(en => en.TimeStamp)))));
            return await ProcessResponse(result);
        }

        public async Task<bool> DeleteIndexAsync(string indexName)
        {
            await Log.InfoAsync($"Delete index {indexName}");
            var result = await _elasticClient
                .Indices
                .DeleteAsync(indexName);
            return await ProcessResponse(result);
        }

        private static async Task<bool> ProcessResponse<T>(T response)
        {
            
            switch (response)
            {
                case DeleteIndexResponse deleteIndexResponse :
                    if (deleteIndexResponse.Acknowledged) return deleteIndexResponse.Acknowledged;
                    await Log.WarningAsync(deleteIndexResponse.DebugInformation);
                    await Log.ErrorAsync(deleteIndexResponse.OriginalException, deleteIndexResponse.ServerError.Error.Reason);
                    return deleteIndexResponse.Acknowledged;
                case IndexResponse indexResponse:
                    if (indexResponse.IsValid) return indexResponse.IsValid;
                    await Log.WarningAsync(indexResponse.DebugInformation);
                    await Log.ErrorAsync(indexResponse.OriginalException, indexResponse.ServerError.Error.Reason);
                    return indexResponse.IsValid;
                case CreateIndexResponse createIndexResponse:
                    if (createIndexResponse.IsValid) return createIndexResponse.IsValid;
                    await Log.WarningAsync(createIndexResponse.DebugInformation);
                    await Log.ErrorAsync(createIndexResponse.OriginalException, createIndexResponse.ServerError.Error.Reason);
                    return createIndexResponse.IsValid;
                case BulkResponse bulkResponse:
                    if (bulkResponse.IsValid)
                    {
                        await Log.InfoAsync($"Successfully write {bulkResponse.Items.Count} documents to elastic");
                        return bulkResponse.IsValid;
                    }
                    await Log.WarningAsync(bulkResponse.DebugInformation);
                    await Log.ErrorAsync(bulkResponse.OriginalException, bulkResponse.ServerError.Error.Reason);
                    return bulkResponse.IsValid;
                default:
                    await Log.WarningAsync($"Cannot find Conversion for type <{response.GetType().Name}>");
                    return false;
            }
        }
        
    }
}