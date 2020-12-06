using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Scheduler
{
    public class SchedulerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<SchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var locations = (List<string>) context.JobDetail.JobDataMap["locations"];
            await Task.Run(async () =>
            {
                await Log.InfoAsync("Use the following parameter for connections:");
                await Log.InfoAsync($"Parallelism: {configuration.Parallelism}");
                await Log.InfoAsync($"Runs every: {configuration.RunsEvery} s");
                await Log.InfoAsync($"Path to Locations file: {configuration.PathToLocationsMap}");
                await Log.InfoAsync($"ElasticSearch: {configuration.ElasticHostsAndPorts}");
                IElasticConnectionBuilder connectionBuilder =
                    new ElasticConnectionBuilder();
                var elasticConnection = connectionBuilder.Build(configuration);

                if (!elasticConnection.IndexExists(configuration.ElasticIndexName))
                {
                    if (elasticConnection.CreateIndex(configuration.ElasticIndexName))
                    {
                        await Log.InfoAsync($"index {configuration.ElasticIndexName} successfully created");
                    }
                    else
                    {
                        await Log.WarningAsync($"error while create index {configuration.ElasticIndexName}");
                    }

                }
                else
                {
                    //elasticConnection.DeleteIndex(configuration.ElasticIndexName);
                }
                
                foreach (var location in locations)
                {
                    new OwmApiReader().ReadDataFromLocation(location, configuration.OwmApiKey
                    ).MatchSome(result =>
                    {
                        var res = JsonSerializer.Deserialize<Root>(result);
                        Console.WriteLine(result);



                    });
                }
                
            });
        }
    }
}