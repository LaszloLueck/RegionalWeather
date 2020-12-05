#pragma warning disable
using Optional;
using Optional.Linq;

#pragma warning restore

namespace RegionalWeather.Configuration
{
    public sealed record ConfigurationItems(string OwmApiKey, int RunsEvery, string PathToLocationsMap,
        int Parallelism);

    public enum EnvEntries
    {
        OwmApiKey,
        RunsEvery,
        PathToLocationsMap,
        Parallelism
    }

    public class ConfigurationBuilder
    {
        private readonly IConfigurationFactory _configurationFactory;

        public ConfigurationBuilder(IConfigurationFactory configurationFactory)
        {
            _configurationFactory = configurationFactory;
        }

        public Option<ConfigurationItems> GetConfiguration()
        {
            return (
                from owmApiKey in _configurationFactory.ReadEnvironmentVariableString(EnvEntries.OwmApiKey)
                from runsEvery in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.RunsEvery)
                from parallelism in _configurationFactory.ReadEnvironmentVariableInt(EnvEntries.Parallelism)
                from pathToLocationsMap in _configurationFactory.ReadEnvironmentVariableString(EnvEntries
                    .PathToLocationsMap)
                select new ConfigurationItems(owmApiKey, runsEvery, pathToLocationsMap, parallelism)
            );
        }
    }
}