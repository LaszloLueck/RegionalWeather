﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseReIndexerWeatherImpl : ProcessingBaseReIndexerWeather
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ProcessingBaseReIndexerWeatherImpl>.GetLogger();

        public ProcessingBaseReIndexerWeatherImpl(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmDocumentConverter,
            IDirectoryUtils directoryUtils) : base(
            elasticConnection, owmDocumentConverter, directoryUtils)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            await Task.Run(async () =>
            {
                var continueWithDirectory = true;
                if (!DirectoryExists(configuration.ReindexLookupPath))
                {
                    await Log.WarningAsync("Reindex lookup directory does not exist. Lets create it");
                    continueWithDirectory = CreateDirectory(configuration.ReindexLookupPath);
                }

                if (continueWithDirectory)
                {
                    var files = GetFilesOfDirectory(configuration.ReindexLookupPath,
                        "FileStorage_*.dat");

                    //Create the indexes from files
                    var distinct = files
                        .Select(file =>
                        BuildIndexName(configuration.ElasticIndexName, GenerateIndexDateFromFileName(file)))
                        .Distinct();

                    var createIndexTasks = distinct
                        .Select(async indexName =>
                        {
                            if (!await IndexExistsAsync(indexName))
                            {
                                await CreateIndexAsync<WeatherLocationDocument>(indexName);
                                await RefreshIndexAsync(indexName);
                            }
                        });

                    await Task.WhenAll(createIndexTasks);

                    var tasks = (from file in files select file)
                        .Select(async file =>
                        {
                            await Log.InfoAsync($"Restore data from file <{file}>");
                            var elements = ReadAllLinesOfFile(file);

                            var convertedElementTasks = elements
                                .Select(async element => await DeserializeObjectAsync<CurrentWeatherBase>(element));

                            var convertedElements = (await Task.WhenAll(convertedElementTasks))
                                .Values();

                            var convertedIndexDocsTasks = convertedElements
                                .Select(async element => await ConvertAsync(element));

                            var convertedIndexDocs = (await Task.WhenAll(convertedIndexDocsTasks)).Values();

                            var indexName = BuildIndexName(configuration.ElasticIndexName,
                                GenerateIndexDateFromFileName(file));

                            convertedIndexDocs
                                .Select((owm, index) => new {owm, index})
                                .GroupBy(g => g.index / 100, o => o.owm)
                                .ToList()
                                .ForEach(async group =>
                                    await BulkWriteDocumentsAsync(group, indexName));

                            await FlushIndexAsync(indexName);
                            await Log.InfoAsync($"Remove the file <{file}> after indexing");
                            await Task.Run(() => DeleteFile(file));
                        });

                    await Task.WhenAll(tasks);
                }
            });
        }

        private static DateTime GenerateIndexDateFromFileName(string fileName)
        {
            var subSeq = fileName.IndexOf("FileStorage_");
            var fn = fileName.Substring(subSeq);


            var newName = fn.Replace("FileStorage_", "").Replace(".dat", "");

            return DateTime.ParseExact(newName, "yyyyMMdd", CultureInfo.InvariantCulture);
        }
    }
}