﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using Serilog;

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
        public string BuildIndexName(string indexName, DateTime shardDatetime);

        public Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument;

        public Task<bool> IndexExistsAsync(string indexName);
        public Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument;
        public Task<bool> DeleteIndexAsync(string indexName);
        public Task<bool> RefreshIndexAsync(string indexName);
        public Task<bool> FlushIndexAsync(string indexName);
    }

    public class ElasticConnection : IElasticConnection
    {
        private readonly ElasticClient _elasticClient;
        private readonly ILogger _logger;

        private static IEnumerable<Uri> BuildServerList(string hostsAndPorts)
        {
            return hostsAndPorts.Split(",").Select(uri => new Uri(uri));
        }

        public ElasticConnection(ConfigurationItems configurationItems)
        {
            var pool = new StaticConnectionPool(BuildServerList(configurationItems.ElasticHostsAndPorts).ToArray());
            var setting = new ConnectionSettings(pool);
            _logger = Log.Logger.ForContext<ElasticConnection>();
            _elasticClient = new ElasticClient(setting);
        }

        public string BuildIndexName(string indexName, DateTime shardDatetime)
        {
            //we receive the indexname in format [-XXYYYY], so we can rebuild the sharding as expected from configuration
            //e.g. mysuperindex[-MMyyyy] would be calculated to mysuperindex-122020 for december of 2020...
            var sw = Stopwatch.StartNew();
            try
            {
                var indexPart = indexName.Substring(0, indexName.IndexOf('['));
                var pattern = indexName.Substring(indexName.IndexOf('[')).Replace("[", string.Empty)
                    .Replace("]", string.Empty);
                var shardDate = pattern.Length > 0 ? shardDatetime.ToString(pattern) : "";
                return indexPart + shardDate;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while converting indexname <{indexName}> with sharding pattern");
                return indexName;
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", MethodBase.GetCurrentMethod().Name,
                    sw.ElapsedMilliseconds);
            }
        }

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _elasticClient.IndexManyAsync(documents, indexName);
                ProcessResponse(result);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms",
                    $"BulkWriteDocumentAsync<{typeof(T)}>", sw.ElapsedMilliseconds);
            }
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Check if index exists");
                var ret = await _elasticClient.Indices.ExistsAsync(indexName);
                return ret.Exists;
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "IndexExistsAsync",
                    sw.ElapsedMilliseconds);
            }
        }

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information($"Create index {indexName} with Mapping");
                var result = await _elasticClient
                    .Indices
                    .CreateAsync(indexName, index => index
                        .Map<T>(x => x.AutoMap()
                            .Properties(d => d.Date(e => e.Name(en => en.TimeStamp)))));
                return ProcessResponse(result);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "CreateIndexAsync",
                    sw.ElapsedMilliseconds);
            }
        }

        public async Task<bool> RefreshIndexAsync(string indexName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information($"Refresh index {indexName}");
                var result = await _elasticClient
                    .Indices
                    .RefreshAsync(indexName);
                return ProcessResponse(result);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "RefreshIndexAsync",
                    sw.ElapsedMilliseconds);
            }
        }

        public async Task<bool> FlushIndexAsync(string indexName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information($"Flush index {indexName}");
                var result = await _elasticClient
                    .Indices
                    .FlushAsync(indexName);
                return ProcessResponse(result);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "FlushIndexAsync",
                    sw.ElapsedMilliseconds);
            }
        }

        public async Task<bool> DeleteIndexAsync(string indexName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information($"Delete index {indexName}");
                var result = await _elasticClient
                    .Indices
                    .DeleteAsync(indexName);
                return ProcessResponse(result);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "DeleteIndexAsync",
                    sw.ElapsedMilliseconds);
            }
        }

        private bool ProcessResponse<T>(T response)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                switch (response)
                {
                    case DeleteIndexResponse deleteIndexResponse:
                        if (deleteIndexResponse.Acknowledged) return deleteIndexResponse.Acknowledged;
                        _logger.Warning(deleteIndexResponse.DebugInformation);
                        _logger.Error(deleteIndexResponse.OriginalException,
                            deleteIndexResponse.ServerError.Error.Reason);
                        return deleteIndexResponse.Acknowledged;
                    case IndexResponse indexResponse:
                        if (indexResponse.IsValid) return indexResponse.IsValid;
                        _logger.Warning(indexResponse.DebugInformation);
                        _logger.Error(indexResponse.OriginalException, indexResponse.ServerError.Error.Reason);
                        return indexResponse.IsValid;
                    case CreateIndexResponse createIndexResponse:
                        if (createIndexResponse.IsValid) return createIndexResponse.IsValid;
                        _logger.Warning(createIndexResponse.DebugInformation);
                        _logger.Error(createIndexResponse.OriginalException,
                            createIndexResponse.ServerError.Error.Reason);
                        return createIndexResponse.IsValid;
                    case FlushResponse flushResponse:
                        if (flushResponse.IsValid) return flushResponse.IsValid;
                        _logger.Warning(flushResponse.DebugInformation);
                        _logger.Error(flushResponse.OriginalException, flushResponse.ServerError.Error.Reason);
                        return flushResponse.IsValid;
                    case RefreshResponse refreshResponse:
                        if (refreshResponse.IsValid) return refreshResponse.IsValid;
                        _logger.Warning(refreshResponse.DebugInformation);
                        _logger.Error(refreshResponse.OriginalException, refreshResponse.ServerError.Error.Reason);
                        return refreshResponse.IsValid;
                    case BulkResponse bulkResponse:
                        if (bulkResponse.IsValid)
                        {
                            _logger.Information(
                                $"Successfully written {bulkResponse.Items.Count} documents to elastic");
                            return bulkResponse.IsValid;
                        }

                        _logger.Warning(bulkResponse.DebugInformation);
                        _logger.Error(bulkResponse.OriginalException, bulkResponse.ServerError.Error.Reason);
                        return bulkResponse.IsValid;
                    default:
                        _logger.Warning($"Cannot find Conversion for type <{response.GetType().Name}>");
                        return false;
                }
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", $"ProcessResponse<{typeof(T)}>",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}