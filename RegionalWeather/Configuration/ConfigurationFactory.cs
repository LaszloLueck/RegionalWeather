using System;
using Optional;
using Serilog;

namespace RegionalWeather.Configuration
{
    public class ConfigurationFactory : IConfigurationFactory
    {
        public Option<string> ReadEnvironmentVariableString(EnvEntries value, bool returnEmptyStringIfNoValue = false)
        {
            //Put some sugar here to tell why the container stops.
            return Environment.GetEnvironmentVariable(value.ToString()).SomeNotNull().Match(
                some: Option.Some,
                none: () =>
                {
                    if (returnEmptyStringIfNoValue)
                        return Option.Some(string.Empty);
                    Log.Information($"No entry found for environment variable {value}");
                    return Option.None<string>();
                }
            );
        }

        public Option<int> ReadEnvironmentVariableInt(EnvEntries value)
        {
            return Environment.GetEnvironmentVariable(value.ToString()).SomeNotNull().Match(
                some: variable => int.TryParse(variable, out var intVariable)
                    ? Option.Some(intVariable)
                    : LogAndReturnNone<int>(value.ToString(), variable),
                none: () =>
                {
                    Log.Warning($"No entry found for environment variable {value}");
                    return Option.None<int>();
                }
            );
        }

        private static Option<T> LogAndReturnNone<T>(string envName, string value)
        {
            Log.Warning($"Cannot convert value {value} for env variable {envName}");
            return Option.None<T>();
        }

        public Option<bool> ReadEnvironmentVariableBool(EnvEntries value)
        {
            return Environment.GetEnvironmentVariable(value.ToString()).SomeNotNull().Match(
                some: variable => bool.TryParse(variable, out var boolVariable)
                    ? Option.Some(boolVariable)
                    : LogAndReturnNone<bool>(value.ToString(), variable),
                none: () =>
                {
                    Log.Warning($"No entry found for environment variable {value}");
                    return Option.None<bool>();
                }
            );
        }
    }
}