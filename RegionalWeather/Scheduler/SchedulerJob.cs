using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;

namespace RegionalWeather.Scheduler
{
    public class SchedulerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<SchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            await Task.Run(async () =>
            {
                await Log.InfoAsync("Use the following parameter for connections:");
                await Log.InfoAsync($"Pihole host: {configuration.OwmApiKey}");
                await Log.InfoAsync($"Pihole telnet port: {configuration.Parallelism}");
                await Log.InfoAsync($"InfluxDb host: {configuration.RunsEvery}");
                await Log.InfoAsync($"InfluxDb port: {configuration.PathToLocationsMap}");

                
            });
        }
    }
}