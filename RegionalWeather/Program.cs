using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Scheduler;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.WithCaller;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;

namespace RegionalWeather
{
    internal static class BuildLoggerConfiguration
    {
        public static Logger Build(Option<ConfigurationItems> configurationOpt)
        {
            return configurationOpt.Match(factory =>
                {
                    var l = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                            outputTemplate:
                            "[{Timestamp:yyy-MM-dd HH:mm:ss} {Level:u4}] {Caller}{NewLine}{Message:lj}{NewLine}{Exception}")
                        .Enrich.FromLogContext()
                        .Enrich.WithCaller();

                    if (factory.LogToElasticSearch)
                    {
                        var elasticUriList = factory.ElasticHostsAndPorts.Split(",").Select(uri => new Uri(uri));
                        return l.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticUriList)
                        {
                            AutoRegisterTemplate = true, AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                            IndexFormat = $"{factory.ElasticSearchLogIndexName}-{DateTime.UtcNow:yyyy-MM}"
                        }).CreateLogger();
                    }

                    return l.CreateLogger();
                },
                () =>
                {
                    return new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                            outputTemplate:
                            "[{Timestamp:yyy-MM-dd HH:mm:ss} {Level:u4}] {Caller}{NewLine}{Message:lj}{NewLine}{Exception}")
                        .Enrich.FromLogContext()
                        .Enrich.WithCaller()
                        .CreateLogger();
                });
        }
    }

    class Program
    {
        protected Program()
        {
        }

        private static ILogger logger;

        static async Task Main(string[] args)
        {
            Log.Logger = BuildLoggerConfiguration.Build(Option.None<ConfigurationItems>());

            logger = Log.Logger.ForContext<Program>();

            logger.Information("starting app");

            IConfigurationFactory configurationFactory = new ConfigurationFactory();

            var startupObjectOpt = await new ConfigurationBuilder(configurationFactory).GetConfigurationAsync();

            var mainTask = startupObjectOpt.Map(configuration =>
            {
                var sw = Stopwatch.StartNew();
                Log.Logger = BuildLoggerConfiguration.Build(Option.Some<ConfigurationItems>(configuration));

                logger.Information("Build up the scheduler");
                try
                {
                    Task.Run(async () =>
                    {
                        ISchedulerFactory currentWeatherSchedulerFactory =
                            new CustomSchedulerFactory<CurrentWeatherSchedulerJob>("currentWeatherJob",
                                "currentWeatherGroup", "currentWeatherTrigger", 10, configuration.RunsEvery,
                                configuration);
                        ISchedulerFactory currentWeatherReindexerFactory =
                            new CustomSchedulerFactory<ReindexerSchedulerJobWeather>("reIndexerJob",
                                "reIndexerGroup",
                                "reIndexerTrigger", 5, configuration.ReindexLookupEvery, configuration);

                        ISchedulerFactory airPollutionSchedulerFactory =
                            new CustomSchedulerFactory<AirPollutionSchedulerJob>("airPollutionJob",
                                "airPollutionGroup",
                                "airPollutionTrigger", 15, configuration.AirPollutionRunsEvery, configuration);

                        ISchedulerFactory airPollutionReindexerFactory =
                            new CustomSchedulerFactory<ReindexerSchedulerJobAirPollution>(
                                "reindexerAirPollutionJob",
                                "reindexerAirPollutionGroup", "reindexerAirPollutionTrigger", 15,
                                configuration.ReindexLookupEvery, configuration);

                        await currentWeatherSchedulerFactory.RunScheduler();
                        await currentWeatherReindexerFactory.RunScheduler();
                        await airPollutionSchedulerFactory.RunScheduler();
                        await airPollutionReindexerFactory.RunScheduler();
                        logger.Information("App is in running state!");
                    });
                    return Task.Delay(-1);
                }
                finally
                {
                    sw.Stop();
                    logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "Main",
                        sw.ElapsedMilliseconds);
                }
            }).ValueOr(() => Task.CompletedTask);


            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}