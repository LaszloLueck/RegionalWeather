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
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseImplementations
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ProcessingBaseImplementations>.GetLogger();
        
        private readonly ElasticConnection _elasticConnection;
        
        protected ProcessingBaseImplementations(ElasticConnection elasticConnection)
        {
            _elasticConnection = elasticConnection;
        }
        
        public async Task<bool> ElasticIndexExistsAsync(string indexName)
        {
            return await _elasticConnection.IndexExistsAsync(indexName);
        }

        public async Task<bool> CreateIndexAsync(string indexName)
        {
            return await _elasticConnection.CreateIndexAsync(indexName);
        }
        
        protected static async Task<Option<T>> DeserializeObjectAsync<T>(string data)
        {
            try
            {
                await using MemoryStream stream = new(16184);
                var bt = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(bt.AsMemory(0, bt.Length));
                stream.Position = 0;
                var retValue = await JsonSerializer.DeserializeAsync<T>(stream).AsTask();
                return retValue != null ? Option.Some<T>(retValue) : Option.None<T>();
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, "Error while converting line to object");
                return await Task.Run(Option.None<T>);
            }
        }
        
        protected static ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism)
        {
            return queryable
                .AsParallel()
                .WithDegreeOfParallelism(parallelism);
        }
        
    }
}