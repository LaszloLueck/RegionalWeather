#pragma warning disable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using Serilog;

#pragma warning restore

namespace RegionalWeather.Configuration
{
    public sealed record ConfigurationItems(string OwmApiKey, int RunsEvery, string PathToLocationsMap,
        int Parallelism, string ElasticHostsAndPorts, string ElasticIndexName, string FileStorageTemplate, 
        int AirPollutionRunsEvery, string AirPollutionIndexName,
        string AirPollutionLocationsFile, string AirPollutionFileStoragePath);

    public enum EnvEntries
    {
        OwmApiKey,
        RunsEvery,
        PathToLocationsMap,
        Parallelism,
        ElasticHostsAndPorts,
        ElasticIndexName,
        FileStorageTemplate,
        AirPollutionRunsEvery,
        AirPollutionIndexName,
        AirPollutionLocationsFile,
        AirPollutionFileStoragePath
    }

    public class ConfigurationBuilder
    {
        private readonly IConfigurationFactory _configurationFactory;
        private readonly ILogger _logger;
        
        public ConfigurationBuilder(IConfigurationFactory configurationFactory)
        {
            _logger = Log.Logger.ForContext<ConfigurationBuilder>();
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
            _logger.Information("Try to read the configuration items from env vars");
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
                from airPollutionRunsEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries
                    .AirPollutionRunsEvery)
                from airPollutionIndexName in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionIndexName)
                from airPollutionLocationsFile in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionLocationsFile)
                from airPollutionFileStoragePath in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .AirPollutionFileStoragePath)
                select new ConfigurationItems(owmApiKey, runsEvery, pathToLocationsMap, parallelism,
                    elasticHostsAndPorts, elasticIndexName, fileStorageTemplate,
                    airPollutionRunsEvery, airPollutionIndexName, airPollutionLocationsFile,
                    airPollutionFileStoragePath);
        }
    }
}