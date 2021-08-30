using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
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
        private readonly IProcessingUtils _processingUtils;
        private readonly IOwmToElasticDocumentConverter<CurrentWeatherBase> _owmToElasticDocumentConverter;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;
        private readonly ILogger _logger;

        public ProcessingBaseCurrentWeatherImpl(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader, IOwmApiReader owmApiReader, IProcessingUtils processingUtils,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmToElasticDocumentConverter,
            IProcessingBaseImplementations processingBaseImplementations)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _owmApiReader = owmApiReader;
            _processingUtils = processingUtils;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
            _processingBaseImplementations = processingBaseImplementations;
            _logger = Log.Logger.ForContext<ProcessingBaseCurrentWeatherImpl>();
            _logger.Information("Begin with etl process of weather information for locations.");
        }

        public async Task Process(ConfigurationItems configuration)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("try to read weatherinformations");
                var locationList =
                    (await _locationFileReader.ReadLocationsAsync(configuration.PathToLocationsMap)).ValueOr(
                        new List<string>());
                _logger.Information($"read the list of locations with {locationList.Count} entries");
                var rootTasksOption = _processingBaseImplementations
                    .ConvertToParallelQuery(locationList, configuration.Parallelism)
                    .Select(async location =>
                    {
                        _logger.Information($"get weather information for configured {location}");
                        var url =
                            $"https://api.openweathermap.org/data/2.5/weather?{location}&APPID={configuration.OwmApiKey}&units=metric";
                        return await _owmApiReader.ReadDataFromLocationAsync(url, location);
                    });

                var rootOptions = (await Task.WhenAll(rootTasksOption)).Values();

                var rootStrings = _processingBaseImplementations
                    .ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                    .Select(async rootString =>
                        await _processingBaseImplementations.DeserializeObjectAsync<CurrentWeatherBase>(rootString));


                var readTime = DateTime.Now;
                _logger.Information($"define document timestamp for elastic is {readTime}");
                var toElastic = (await Task.WhenAll(rootStrings))
                    .Values()
                    .Select(item =>
                    {
                        item.ReadTime = readTime;
                        item.Guid = Guid.NewGuid();
                        return item;
                    });

                var concurrentBag = new ConcurrentBag<CurrentWeatherBase>(toElastic);

                await _processingUtils.WriteFilesToDirectory(configuration.FileStorageTemplate, concurrentBag);

                var elasticDocsTasks = _processingBaseImplementations
                    .ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
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
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "ProcessingBaseCurrentWeatherImpl.Execute",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}