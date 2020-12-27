using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RegionalWeather.Logging;

namespace RegionalWeather.Reindexing
{
    public class DirectoryUtils : IDirectoryUtils
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<DirectoryUtils>.GetLogger();
        
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public async Task<IEnumerable<string>> ReadAllLinesOfFileAsync(string path)
        {
            return await File.ReadAllLinesAsync(path);
        }

        public bool CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"error while creating the directory {path}");
                return false;
            }
        }
        
        

        public IEnumerable<string> GetFilesOfDirectory(string path, string filePattern)
        {
            try
            {
                return Directory.GetFiles(path, filePattern);
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"error while getting files in directory {path} with pattern {filePattern}");
                return new List<string>();
            }
        }

        public bool DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"error while deleting a file {path}");
                return false;
            }
        }
    }
}