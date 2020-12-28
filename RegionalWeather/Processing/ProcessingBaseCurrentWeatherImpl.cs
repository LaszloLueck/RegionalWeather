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
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseCurrentWeatherImpl : ProcessingBaseCurrentWeather
    {
        public ProcessingBaseCurrentWeatherImpl(IElasticConnection elasticConnection,
            ILocationFileReaderImpl locationFileReader, IOwmApiReader owmApiReader, IFileStorageImpl fileStorageImpl,
            IOwmToElasticDocumentConverter owmToElasticDocumentConverter) : base(elasticConnection, locationFileReader,
            owmApiReader, fileStorageImpl, owmToElasticDocumentConverter)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            var elasticIndexSuccess = true;
            if (!await IndexExistsAsync(configuration.ElasticIndexName))
            {
                elasticIndexSuccess = await CreateIndexAsync(configuration.ElasticIndexName);
            }

            if (elasticIndexSuccess)
            {
                var locationList = (await ReadConfigurationAsync()).ValueOr(new List<string>());


                var rootTasksOption = ConvertToParallelQuery(locationList, configuration.Parallelism)
                    .Select(async location =>
                    {
                        var url =
                            $"https://api.openweathermap.org/data/2.5/weather?{location}&APPID={configuration.OwmApiKey}&units=metric";
                        return await ReadDataFromLocationAsync(url);
                    });

                var rootOptions = (await Task.WhenAll(rootTasksOption)).Values();

                var rootStrings = ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                    .Select(async rootString => await DeserializeObjectAsync<Root>(rootString));

                var toElastic = (await Task.WhenAll(rootStrings))
                    .Values()
                    .Select(item =>
                    {
                        item.ReadTime = DateTime.Now;
                        return item;
                    });

                var concurrentBag = new ConcurrentBag<Root>(toElastic);
                await WriteAllDataAsync(concurrentBag);

                var elasticDocsTasks = ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
                    .Select(async rootDoc => await ConvertAsync(rootDoc));

                var elasticDocs = (await Task.WhenAll(elasticDocsTasks))
                    .Values();

                await BulkWriteDocumentsAsync(elasticDocs, configuration.ElasticIndexName);
            }
            
        }
    }
}