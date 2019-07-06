using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public enum LogLevel
    {
        Debug,
        All = Debug,
        Info,
        Warn,
        Error,
        Fatal,
        None = int.MaxValue,
    }

    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
    }

    public static class Log
    {
        public static Action<string> OutDebug { get; set; } = (s) =>
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(s);
            Console.ForegroundColor = currentColor;
        };

        public static Action<string> OutInfo { get; set; } = (s) =>
         {
             Console.WriteLine(s);
         };

        public static Action<string> OutWarn { get; set; } = (s) =>
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(s);
            Console.ForegroundColor = currentColor;
        };

        public static Action<string> OutError { get; set; } = (s) =>
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ForegroundColor = currentColor;
        };

        public static Action<string> OutFatal { get; set; } = (s) =>
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(s);
            Console.ForegroundColor = currentColor;
        };

        public static void Debug(string logstring)
        {
            OutDebug(logstring);
        }

        public static void Info(string logstring)
        {
            OutInfo(logstring);
        }

        public static void Warn(string logstring)
        {
            OutWarn(logstring);
        }

        public static void Error(string logstring)
        {
            OutError(logstring);
        }

        public static void Fatal(string logstring)
        {
            OutFatal(logstring);
        }
    }
}
