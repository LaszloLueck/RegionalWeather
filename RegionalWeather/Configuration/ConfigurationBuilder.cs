#pragma warning disable
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using RegionalWeather.Logging;

#pragma warning restore

namespace RegionalWeather.Configuration
{
    public sealed record ConfigurationItems(string OwmApiKey, int RunsEvery, string PathToLocationsMap,
        int Parallelism, string ElasticHostsAndPorts, string ElasticIndexName, string FileStorageTemplate,
        int ReindexLookupEvery, string ReindexLookupPath);

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
        ReindexLookupPath
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
            return await Task.Run(GetConfiguration);
        }
        

        public Option<ConfigurationItems> GetConfiguration()
        {
            Log.Info("Try to read the configuration items from env vars");
            return 
                from owmApiKey in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.OwmApiKey)
                from runsEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.RunsEvery)
                from parallelism in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.Parallelism)
                from pathToLocationsMap in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .PathToLocationsMap)
                from elasticHostsAndPorts in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.ElasticHostsAndPorts)
                from elasticIndexName in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.ElasticIndexName)
                from fileStorageTemplate in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.FileStorageTemplate)
                from reindexLookupEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.ReindexLookupEvery)
                from reindexLookupPath in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.ReindexLookupPath)
                select new ConfigurationItems(owmApiKey, runsEvery, pathToLocationsMap, parallelism, elasticHostsAndPorts, elasticIndexName, fileStorageTemplate,reindexLookupEvery,reindexLookupPath);
        }
    }
}