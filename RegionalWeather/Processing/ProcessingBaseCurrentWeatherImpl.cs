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
using RegionalWeather.Owm;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseCurrentWeatherImpl : ProcessingBaseCurrentWeather
    {
        public ProcessingBaseCurrentWeatherImpl(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader, IOwmApiReader owmApiReader, IFileStorage fileStorage,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmToElasticDocumentConverter) : base(elasticConnection,
            locationFileReader,
            owmApiReader, fileStorage, owmToElasticDocumentConverter)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            var elasticIndexSuccess = true;
            if (!await IndexExistsAsync(configuration.ElasticIndexName))
            {
                elasticIndexSuccess = await CreateIndexAsync<WeatherLocationDocument>(configuration.ElasticIndexName);
            }

            if (elasticIndexSuccess)
            {
                var locationList =
                    (await ReadLocationsAsync(configuration.PathToLocationsMap)).ValueOr(new List<string>());
                var rootTasksOption = ConvertToParallelQuery(locationList, configuration.Parallelism)
                    .Select(async location =>
                    {
                        var url =
                            $"https://api.openweathermap.org/data/2.5/weather?{location}&APPID={configuration.OwmApiKey}&units=metric";
                        return await ReadDataFromLocationAsync(url);
                    });

                var rootOptions = (await Task.WhenAll(rootTasksOption)).Values();

                var rootStrings = ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                    .Select(async rootString => await DeserializeObjectAsync<CurrentWeatherBase>(rootString));

                var toElastic = (await Task.WhenAll(rootStrings))
                    .Values()
                    .Select(item =>
                    {
                        item.ReadTime = DateTime.Now;
                        return item;
                    });

                var concurrentBag = new ConcurrentBag<CurrentWeatherBase>(toElastic);
                var storeFileName =
                    configuration.FileStorageTemplate.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyMMdd"));

                await WriteAllDataAsync(concurrentBag, storeFileName);

                var elasticDocsTasks = ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
                    .Select(async rootDoc => await ConvertAsync(rootDoc));

                var elasticDocs = (await Task.WhenAll(elasticDocsTasks))
                    .Values();

                await BulkWriteDocumentsAsync(elasticDocs, configuration.ElasticIndexName);
            }
        }
    }
}