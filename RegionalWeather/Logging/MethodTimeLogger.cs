using System;
using System.Reflection;
using Serilog;

namespace RegionalWeather.Logging
{
    public static class MethodTimeLogger
    {

        
        public static void Log(MethodBase methodBase, TimeSpan elapsed, string message)
        {

            //Do some logging here
            Console.WriteLine($"{methodBase.Name}: {elapsed} ms. {message}");
            
        }
    }
}