using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using Serilog;
using Serilog.Core;

namespace RegionalWeather.Scheduler
{
    public class CustomSchedulerFactory<T> : ISchedulerFactory where T : class, IJob
    {
        private readonly ILogger _logger;
        private readonly string _jobName;
        private readonly string _groupName;
        private readonly string _triggerName;
        private IScheduler _scheduler;
        private readonly StdSchedulerFactory _factory;
        private readonly ConfigurationItems _configurationItems;
        private readonly ILogger _loggingBase;
        private readonly int _runsEvery;
        private readonly int _delay;

        public CustomSchedulerFactory(string jobName, string groupName, string triggerName, int delay, int runsEvery,
            ConfigurationItems configurationItems, ILogger loggingBase)
        {
            _logger = loggingBase.ForContext<CustomSchedulerFactory<T>>();

            _logger.Information("Generate Scheduler with Values: ");
            _logger.Information($"JobName: {jobName}");
            _logger.Information($"GroupName: {groupName}");
            _logger.Information($"TriggerName: {triggerName}");
            _logger.Information($"RepeatInterval: {runsEvery} s");
            _logger.Information($"Start delay: {delay} s");
            _jobName = jobName;
            _groupName = groupName;
            _triggerName = triggerName;
            _configurationItems = configurationItems;
            _loggingBase = loggingBase;
            _runsEvery = runsEvery;
            _delay = delay;
            _factory = new StdSchedulerFactory();
        }

        public async Task RunScheduler()
        {
            _logger.Information("Initialize the scheduler.");
            await BuildScheduler();
            await StartScheduler();
            await ScheduleJob();
        }

        private async Task BuildScheduler()
        {
            _logger.Information("Build Scheduler");
            _scheduler = await _factory.GetScheduler();
        }

        private IJobDetail GetJob()
        {
            return JobBuilder
                .Create<T>()
                .WithIdentity(_jobName, _groupName)
                .Build();
        }

        private ITrigger GetTrigger()
        {
            var dto = new DateTimeOffset(DateTime.Now).AddSeconds(_delay);
            return TriggerBuilder
                .Create()
                .WithIdentity(_triggerName, _groupName)
                .StartAt(dto)
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(_runsEvery).RepeatForever())
                .Build();
        }

        private async Task StartScheduler()
        {
            _logger.Information("Start Scheduler");
            await _scheduler.Start();
        }

        private async Task ScheduleJob()
        {
            var job = GetJob();
            var trigger = GetTrigger();
            job.JobDataMap.Put("configuration", _configurationItems);
            job.JobDataMap.Put("loggingBase", _loggingBase);
            _logger.Information("Schedule Job");
            await _scheduler.ScheduleJob(job, trigger);
        }

        public async Task ShutdownScheduler()
        {
            _logger.Information("Shutdown Scheduler");
            await _scheduler.Shutdown();
        }
    }
}