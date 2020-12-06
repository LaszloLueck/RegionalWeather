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
                await Log.InfoAsync($"Pihole host: {configuration.OwmApiKey}");
                await Log.InfoAsync($"Pihole telnet port: {configuration.Parallelism}");
                await Log.InfoAsync($"InfluxDb host: {configuration.RunsEvery}");
                await Log.InfoAsync($"InfluxDb port: {configuration.PathToLocationsMap}");

                foreach (var location in locations)
                {
                    new OwmApiReader().ReadDataFromLocation(location, configuration.OwmApiKey
                    ).MatchSome(result =>
                    {
                        var res = JsonSerializer.Deserialize<Root>(result);
                        Console.WriteLine(result);
                        var node = new Uri("http://myserver:9200");
                        var settings = new ConnectionSettings(node);
                        var client = new ElasticClient(settings);
                        
                        
                    });
                }
                
            });
        }
    }
}