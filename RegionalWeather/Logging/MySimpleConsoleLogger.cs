#nullable enable
using System;
using System.Threading.Tasks;

namespace RegionalWeather.Logging
{
    public interface IMySimpleLogger
    {
        Task InfoAsync(string? message, string type = "INFO");

        Task ErrorAsync(Exception? ex, string? message);

        Task WarningAsync(string? message);

        void Info(string? message, string type = "INFO");

        void Error(Exception? ex, string? message);

    }


    public class MySimpleConsoleLogger<T> : IMySimpleLogger
    {

        public async Task InfoAsync(string? message, string type ="INFO")
        {
            await Console.Out.WriteLineAsync($"{DateTime.Now} {type} :: {typeof(T).Name} : {message}");
        }

        public void Info(string? message, string type = "INFO")
        {
            Console.WriteLine($"{DateTime.Now} {type} :: {typeof(T).Name} : {message}");
        }

        public void Error(Exception? ex, string? message)
        {
            var defaultConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Info(message, "ERROR");
            Console.WriteLine($"{DateTime.Now} ERROR :: {typeof(T).Name} : {ex?.Message}");
            Console.WriteLine($"{DateTime.Now} ERROR :: {typeof(T).Name} : {ex?.StackTrace}");
            Console.ForegroundColor = defaultConsoleColor;
        }

        public Task ErrorAsync(Exception? ex, string? message)
        {
            return Task.Run(async () =>
            {
                var defaultConsoleColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                await InfoAsync(message, "ERROR");
                await Console.Out.WriteLineAsync($"{DateTime.Now} ERROR :: {typeof(T).Name} : {ex?.Message}");
                await Console.Out.WriteLineAsync($"{DateTime.Now} ERROR :: {typeof(T).Name} : {ex?.StackTrace}");
                Console.ForegroundColor = defaultConsoleColor;
            });
        }

        public Task WarningAsync(string? message)
        {
            return Task.Run(async () =>
            {
                await InfoAsync(message, "WARN");
            });
        }
    }

    public static class MySimpleLoggerImpl<T> where T : class
    {
        public static IMySimpleLogger GetLogger()
        {
            return new MySimpleConsoleLogger<T>();
        }
    }
    
    
}