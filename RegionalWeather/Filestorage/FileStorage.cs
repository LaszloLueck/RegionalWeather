using System;
using System.IO;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;

namespace RegionalWeather.Filestorage
{
    interface IFileStorage
    {
        FileStorageImpl Build(ConfigurationItems configurationItems);
    }

    public class FileStorage : IFileStorage
    {
        public FileStorageImpl Build(ConfigurationItems configurationItems)
        {
            return new(configurationItems);
        }
    }

    public class FileStorageImpl
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<FileStorageImpl>.GetLogger();
        private readonly StreamWriter _sw;
        public FileStorageImpl(ConfigurationItems configurationItems)
        {
            var filename = configurationItems.FileStorageTemplate.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyddMM"));
            _sw = new StreamWriter(filename, true);
        }

        public void FlushData()
        {
            _sw.Flush();
        }

        public void CloseFileStream()
        {
            _sw.Close();
            _sw.Dispose();
        }
        

        public Option<bool> WriteData(string data)
        {
            try
            {
                _sw.WriteLine(data);
                return Option.Some(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while writing data to file");
                return Option.None<bool>();
            }
        }
        
    }
}