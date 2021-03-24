using Optional;

namespace RegionalWeather.Configuration
{
    public interface IConfigurationFactory
    {
        Option<string> ReadEnvironmentVariableString(EnvEntries value, bool returnEmptyStringIfNoValue = false);
        Option<int> ReadEnvironmentVariableInt(EnvEntries value);
        Option<bool> ReadEnvironmentVariableBool(EnvEntries value);
    }
}