#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Logging;
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

        public ProcessingBaseImplementations(ILogger loggingBase)
        {
            _logger = loggingBase.ForContext<ProcessingBaseImplementations>();
        }
        
        public async Task<Option<T>> DeserializeObjectAsync<T>(string data)
        {
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
        }
        
        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism)
        {
            _logger.Information($"create parallel query from type ${typeof(T)} with parallelism {parallelism}");
            return queryable
                .AsParallel()
                .WithDegreeOfParallelism(parallelism);
        }
        
    }
}