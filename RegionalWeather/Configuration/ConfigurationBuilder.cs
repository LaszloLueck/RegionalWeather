#pragma warning disable
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using RegionalWeather.Logging;

#pragma warning restore

namespace RegionalWeather.Configuration
{
    public sealed record ConfigurationItems(string OwmApiKey, int RunsEvery, string PathToLocationsMap,
        int Parallelism, string ElasticHostsAndPorts, string ElasticIndexName, string FileStorageTemplate,
        int ReindexLookupEvery, string ReindexLookupPath, int AirPollutionRunsEvery, string AirPollutionIndexName,
        string AirPollutionLocationsFile, string AirPollutionFileStoragePath, bool LogToElasticSearch,
        string ElasticSearchLogIndexName);

    public enum EnvEntries
    {
        OwmApiKey,
        RunsEvery,
        PathToLocationsMap,
        Parallelism,
        ElasticHostsAndPorts,
        ElasticIndexName,
        FileStorageTemplate,
        ReindexLookupEvery,
        ReindexLookupPath,
        AirPollutionRunsEvery,
        AirPollutionIndexName,
        AirPollutionLocationsFile,
        AirPollutionFileStoragePath,
        LogToElasticSearch,
        ElasticSearchLogIndexName
    }

    public class ConfigurationBuilder
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ConfigurationBuilder>.GetLogger();
        private readonly IConfigurationFactory _configurationFactory;

        public ConfigurationBuilder(IConfigurationFactory configurationFactory)
        {
            _configurationFactory = configurationFactory;
        }

        public async Task<Option<ConfigurationItems>> GetConfigurationAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return await Task.Run(GetConfiguration);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"GetConfigurationAsync :: {sw.ElapsedMilliseconds} ms");
            }
        }


        private Option<ConfigurationItems> GetConfiguration()
        {
            Log.Info("Try to read the configuration items from env vars");
            return
                from owmApiKey in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.OwmApiKey)
                from runsEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.RunsEvery)
                from parallelism in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.Parallelism)
                from pathToLocationsMap in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .PathToLocationsMap)
                from elasticHostsAndPorts in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .ElasticHostsAndPorts)
                from elasticIndexName in _configurationFactory.ReadEnvironmentVariableString(
                    EnvEntries.ElasticIndexName)
                from fileStorageTemplate in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .FileStorageTemplate)
                from reindexLookupEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries
                    .ReindexLookupEvery)
                from reindexLookupPath in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .ReindexLookupPath)
                from airPollutionRunsEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries
                    .AirPollutionRunsEvery)
                from airPollutionIndexName in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionIndexName)
                from airPollutionLocationsFile in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionLocationsFile)
                from airPollutionFileStoragePath in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionFileStoragePath)
                from logToElasticSearch in _configurationFactory.ReadEnvironmentVariableBool(EnvEntries
                    .LogToElasticSearch)
                from elasticSearchLogIndexName in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .ElasticSearchLogIndexName)
                select new ConfigurationItems(owmApiKey, runsEvery, pathToLocationsMap, parallelism,
                    elasticHostsAndPorts, elasticIndexName, fileStorageTemplate, reindexLookupEvery, reindexLookupPath,
                    airPollutionRunsEvery, airPollutionIndexName, airPollutionLocationsFile,
                    airPollutionFileStoragePath, logToElasticSearch, elasticSearchLogIndexName);
        }
    }
}