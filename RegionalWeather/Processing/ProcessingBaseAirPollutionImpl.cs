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
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseAirPollutionImpl : ProcessingBaseAirPollution
    {
        public ProcessingBaseAirPollutionImpl(IElasticConnection elasticConnection,
            ILocationFileReaderImpl locationFileReaderImplImpl, IFileStorageImpl fileStorageImplImpl,
            IOwmApiReader owmApiReader, IOwmToElasticDocumentConverter<AirPollutionBase> owmToElasticDocumentConverter)
            : base(elasticConnection, locationFileReaderImplImpl, fileStorageImplImpl,
                owmApiReader, owmToElasticDocumentConverter)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            var elasticIndexSuccess = true;
            if (!await IndexExistsAsync(configuration.AirPollutionIndexName))
            {
                elasticIndexSuccess = await CreateIndexAsync<AirPollutionDocument>(configuration.AirPollutionIndexName);
            }

            if (elasticIndexSuccess)
            {
                var locationsList =
                    (await ReadLocationsAsync(configuration.AirPollutionLocationsFile)).ValueOr(new List<string>());

                var splitLocationList = locationsList.Select(element =>
                {
                    var splt = element.Split(";");
                    return (splt[0], splt[1]);
                });

                var rootTasks = ConvertToParallelQuery(splitLocationList, configuration.Parallelism)
                    .Select(async location =>
                    {
                        var uri =
                            $"https://api.openweathermap.org/data/2.5/air_pollution?{location.Item1}&appid={configuration.OwmApiKey}";
                        var resultOpt = await ReadDataFromLocationAsync(uri);
                        return resultOpt.Map(result => (location.Item2, result));
                    });

                var rootOptions = (await Task.WhenAll(rootTasks)).Values();

                var rootStrings = ConvertToParallelQuery(rootOptions, configuration.Parallelism)
                    .Select(async rootElement =>
                    {
                        
                        var elementOpt = await DeserializeObjectAsync<AirPollutionBase>(rootElement.Item2);

                        return elementOpt.Map(element =>
                        {
                            element.LocationName = rootElement.Item1;
                            element.ReadTime = DateTime.Now;
                            return element;
                        });
                    });

                var toElastic = (await Task.WhenAll(rootStrings)).Values();

                var concurrentBag = new ConcurrentBag<AirPollutionBase>(toElastic);
                var storeFileName =
                    configuration.AirPollutionFileStoragePath.Replace("[CURRENTDATE]",
                        DateTime.Now.ToString("yyyyMMdd"));

                await WriteAllDataAsync(concurrentBag, storeFileName);

                var elasticDocTasks = ConvertToParallelQuery(concurrentBag, configuration.Parallelism)
                    .Select(async apDoc => await ConvertAsync(apDoc));

                var elasticDocs = (await Task.WhenAll(elasticDocTasks)).Values();

                await BulkWriteDocumentsAsync(elasticDocs, configuration.AirPollutionIndexName);


            }
        }
    }
}