﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Optional;
using Serilog;

namespace RegionalWeather.Processing
{
    public interface IProcessingBaseImplementations
    {
        public Task<Option<T>> DeserializeObjectAsync<T>(string data);

        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism);
    }

    public class ProcessingBaseImplementations : IProcessingBaseImplementations
    {
        private readonly ILogger _logger;

        public ProcessingBaseImplementations()
        {
            _logger = Log.Logger.ForContext<ProcessingBaseImplementations>();
        }

        public async Task<Option<T>> DeserializeObjectAsync<T>(string data)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using MemoryStream stream = new();
                var bt = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(bt.AsMemory(0, bt.Length));
                stream.Position = 0;
                var retValue = await JsonSerializer.DeserializeAsync<T>(stream).AsTask();
                return retValue != null ? Option.Some<T>(retValue) : Option.None<T>();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while converting line to object");
                return await Task.Run(Option.None<T>);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms",
                    $"DeserializeObjectAsync<{typeof(T)}>",
                    sw.ElapsedMilliseconds);
            }
        }

        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information($"create parallel query from type ${typeof(T)} with parallelism {parallelism}");
                return queryable
                    .AsParallel()
                    .WithDegreeOfParallelism(parallelism);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms",
                    $"ConvertToParallelQuery<{typeof(T)}>",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}