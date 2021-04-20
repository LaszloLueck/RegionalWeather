using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Serilog;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseAirPollutionImpl
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReader _locationFileReader;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IProcessingUtils _processingUtils;
        private readonly IOwmToElasticDocumentConverter<AirPollutionBase> _owmToElasticDocumentConverter;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;
        private readonly ILogger _logger;

        public ProcessingBaseAirPollutionImpl(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader, IProcessingUtils processingUtils,
            IOwmApiReader owmApiReader, IOwmToElasticDocumentConverter<AirPollutionBase> owmToElasticDocumentConverter,
            IProcessingBaseImplementations processingBaseImplementations, ILogger loggingBase)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _owmApiReader = owmApiReader;
            _processingUtils = processingUtils;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
            _processingBaseImplementations = processingBaseImplementations;
            _logger = loggingBase.ForContext<ProcessingBaseAirPollutionImpl>();
            _logger.Information("Begin with etl process of weather information for locations.");
        }

        public async Task Process(ConfigurationItems configuration)
        {
            var locationsList =
                (await _locationFileReader.ReadLocationsAsync(configuration.AirPollutionLocationsFile)).ValueOr(
                    new List<string>());
            _logger.Information($"read the list of locations with {locationsList.Count} entries");
            var splitLocationList = locationsList.Select(element =>
            {
                var splt = element.Split(";");
                return (splt[0], splt[1]);
            });

            var rootTasks = _processingBaseImplementations
                .ConvertToParallelQuery(splitLocationList, configuration.Parallelism)
                .Select(async location =>
                {
                    _logger.Information($"get weather information for configured {location}");
                    var uri =
                        $"https://api.openweathermap.org/data/2.5/air_pollution?{location.Item1}&appid={configuration.OwmApiKey}";
                    var resultOpt = await _owmApiReader.ReadDataFromLocationAsync(uri);
                    return resultOpt.Map(result => (location.Item2, result));
                });

            var rootOptions = (await Task.WhenAll(rootTasks)).Values();

            var readTime = DateTime.Now;
            _logger.Information($"define document timestamp for elastic is {readTime}");
            var rootStrings = _processingBaseImplementations
                .ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                .Select(async rootElement =>
                {
                    var elementOpt =
                        await _processingBaseImplementations
                            .DeserializeObjectAsync<AirPollutionBase>(rootElement.Item2);

                    return elementOpt.Map(element =>
                    {
                        element.LocationName = rootElement.Item1;
                        element.ReadTime = readTime;
                        return element;
                    });
                });

            var toElastic = (await Task.WhenAll(rootStrings)).Values();

            var concurrentBag = new ConcurrentBag<AirPollutionBase>(toElastic);

            await _processingUtils.WriteFilesToDirectory(configuration.AirPollutionFileStoragePath, concurrentBag);

            var elasticDocTasks = _processingBaseImplementations
                .ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
                .Select(async apDoc => await _owmToElasticDocumentConverter.ConvertAsync(apDoc));

            var elasticDocs = (await Task.WhenAll(elasticDocTasks)).Values();


            var indexName = _elasticConnection.BuildIndexName(configuration.AirPollutionIndexName, readTime);
            _logger.Information($"write weather data to index {indexName}");
            if (!await _elasticConnection.IndexExistsAsync(indexName))
            {
                await _elasticConnection.CreateIndexAsync<AirPollutionDocument>(indexName);
                await _elasticConnection.RefreshIndexAsync(indexName);
                await _elasticConnection.FlushIndexAsync(indexName);
            }

            await _elasticConnection.BulkWriteDocumentsAsync(elasticDocs, indexName);
        }
    }
}