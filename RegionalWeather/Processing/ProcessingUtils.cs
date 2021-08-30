using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using RegionalWeather.Filestorage;
using Serilog;

namespace RegionalWeather.Processing
{
    public interface IProcessingUtils
    {
        public Task WriteFilesToDirectory<T>(string originalFilePath, ConcurrentBag<T> fileList);
    }


    public class ProcessingUtils : IProcessingUtils
    {
        private readonly IFileStorage _fileStorage;
        private readonly ILogger _logger;

        public ProcessingUtils(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
            _logger = Log.Logger.ForContext<ProcessingUtils>();
        }

        public Task WriteFilesToDirectory<T>(string originalFilePath, ConcurrentBag<T> fileList)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var absoluteFilePath = _fileStorage.GetAbsoluteFilePath(originalFilePath);
                var absoluteDirectoryPath = _fileStorage.GetAbsoluteDirectoryPath(absoluteFilePath);
                var storeFileName =
                    absoluteFilePath.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyMMdd"));
                _logger.Information($"check if storage directory <{storeFileName}> exists, if not try to create it.");
                var directoryExists = _fileStorage.DirectoryExists(absoluteDirectoryPath) ||
                                      _fileStorage.CreateDirectory(absoluteDirectoryPath);

                if (directoryExists)
                {
                    return _fileStorage.WriteAllDataAsync(fileList, storeFileName);
                }

                _logger.Warning(
                    $"cannot write files to path {storeFileName}, directory does not exists and cannot be created!");
                return Task.CompletedTask;
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "WriteFilesToDirectory",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}