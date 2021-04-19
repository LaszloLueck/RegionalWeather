using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Serilog;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseCurrentWeatherImpl
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReader _locationFileReader;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IFileStorage _fileStorage;
        private readonly IOwmToElasticDocumentConverter<CurrentWeatherBase> _owmToElasticDocumentConverter;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;
        private readonly ILogger _logger;

        public ProcessingBaseCurrentWeatherImpl(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader, IOwmApiReader owmApiReader, IFileStorage fileStorage,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmToElasticDocumentConverter,
            IProcessingBaseImplementations processingBaseImplementations, ILogger loggingBase)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _owmApiReader = owmApiReader;
            _fileStorage = fileStorage;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
            _processingBaseImplementations = processingBaseImplementations;
            _logger = loggingBase.ForContext<ProcessingBaseCurrentWeatherImpl>();
            _logger.Information("Begin with etl process of weather information for locations.");
        }

        public async Task Process(ConfigurationItems configuration)
        {
            _logger.Information("try to read weatherinformations");
            var locationList =
                (await _locationFileReader.ReadLocationsAsync(configuration.PathToLocationsMap)).ValueOr(new List<string>());
            var rootTasksOption = _processingBaseImplementations.ConvertToParallelQuery(locationList, configuration.Parallelism)
                .Select(async location =>
                {
                    _logger.Information($"get weather information for configured {location}");
                    var url =
                        $"https://api.openweathermap.org/data/2.5/weather?{location}&APPID={configuration.OwmApiKey}&units=metric";
                    return await _owmApiReader.ReadDataFromLocationAsync(url);
                });

            var rootOptions = (await Task.WhenAll(rootTasksOption)).Values();

            var rootStrings = _processingBaseImplementations.ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                .Select(async rootString => await _processingBaseImplementations.DeserializeObjectAsync<CurrentWeatherBase>(rootString));


            var readTime = DateTime.Now;
            _logger.Information($"define document timestamp for elastic is {readTime}");
            var toElastic = (await Task.WhenAll(rootStrings))
                .Values()
                .Select(item =>
                {
                    item.ReadTime = readTime;
                    return item;
                });

            var concurrentBag = new ConcurrentBag<CurrentWeatherBase>(toElastic);
            
            var storeFileName =
                configuration.FileStorageTemplate.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyMMdd"));

            await _fileStorage.WriteAllDataAsync(concurrentBag, storeFileName);

            var elasticDocsTasks = _processingBaseImplementations.ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
                .Select(async rootDoc => await _owmToElasticDocumentConverter.ConvertAsync(rootDoc));

            var elasticDocs = (await Task.WhenAll(elasticDocsTasks))
                .Values();

            var indexName = _elasticConnection.BuildIndexName(configuration.ElasticIndexName, readTime);
            _logger.Information($"write weather data to index {indexName}");
            
            if (!await _elasticConnection.IndexExistsAsync(indexName))
            {
                await _elasticConnection.CreateIndexAsync<WeatherLocationDocument>(indexName);
                await _elasticConnection.RefreshIndexAsync(indexName);
                await _elasticConnection.FlushIndexAsync(indexName);
            }

            await _elasticConnection.BulkWriteDocumentsAsync(elasticDocs, indexName);
        }
    }
}