﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Serilog;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseAirPollution : IProcessingBaseImplementations, IElasticConnection,
        ILocationFileReader, IProcessingBase, IOwmApiReader, IFileStorage,
        IOwmToElasticDocumentConverter<AirPollutionBase>
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReader _locationFileReader;
        private readonly IFileStorage _fileStorage;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IOwmToElasticDocumentConverter<AirPollutionBase> _owmToElasticDocumentConverter;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;

        protected ProcessingBaseAirPollution(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader, IFileStorage fileStorage,
            IOwmApiReader owmApiReader, IOwmToElasticDocumentConverter<AirPollutionBase> owmToElasticDocumentConverter, IProcessingBaseImplementations processingBaseImplementations)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _fileStorage = fileStorage;
            _owmApiReader = owmApiReader;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
            _processingBaseImplementations = processingBaseImplementations;
        }

        public Task<Option<T>> DeserializeObjectAsync<T>(string data) =>
            _processingBaseImplementations.DeserializeObjectAsync<T>(data);

        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism) =>
            _processingBaseImplementations.ConvertToParallelQuery(queryable, parallelism);

        public Task<bool> RefreshIndexAsync(string indexName) => _elasticConnection.RefreshIndexAsync(indexName);

        public Task<bool> FlushIndexAsync(string indexName) => _elasticConnection.FlushIndexAsync(indexName);

        public string BuildIndexName(string indexName, DateTime shardDatetime) =>
            _elasticConnection.BuildIndexName(indexName, shardDatetime);

        public async Task<Option<ElasticDocument>> ConvertAsync(AirPollutionBase owmDoc) =>
            await _owmToElasticDocumentConverter.ConvertAsync(owmDoc);

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath) =>
            await _locationFileReader.ReadLocationsAsync(locationPath);

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName) =>
            await _fileStorage.WriteAllDataAsync(roots, fileName);

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument => await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument =>
            await _elasticConnection.CreateIndexAsync<AirPollutionDocument>(indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public abstract Task Process(ConfigurationItems configuration);

        public async Task<Option<string>> ReadDataFromLocationAsync(string url) =>
            await _owmApiReader.ReadDataFromLocationAsync(url);
    }
}