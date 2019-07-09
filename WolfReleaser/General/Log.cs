using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public enum LogLevel
    {
        All,
        Debug,
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
        private static LogLevel conLvl = LogLevel.All;

        public static EventHandler<LogEntry> Logged;

        public static void Debug(string logstring)
        {
            Logged?.Invoke(null, new LogEntry
            {
                Level = LogLevel.Debug,
                Message = logstring
            });
        }

        public static void Info(string logstring)
        {
            Logged?.Invoke(null, new LogEntry
            {
                Level = LogLevel.Info,
                Message = logstring
            });
        }

        public static void Warn(string logstring)
        {
            Logged?.Invoke(null, new LogEntry
            {
                Level = LogLevel.Warn,
                Message = logstring
            });
        }

        public static void Error(string logstring)
        {
            Logged?.Invoke(null, new LogEntry
            {
                Level = LogLevel.Error,
                Message = logstring
            });
        }

        public static void Fatal(string logstring)
        {
            Logged?.Invoke(null, new LogEntry
            {
                Level = LogLevel.Fatal,
                Message = logstring
            });
        }

        public static void SetConsoleLogging(bool onoff, LogLevel level)
        {
            conLvl = level;
            Logged -= Log_Logged;

            if (onoff)
            {
                Logged += Log_Logged;
            }
        }

        private static void Log_Logged(object _, LogEntry log)
        {
            if ((int)conLvl > (int)log.Level)
            {
                return;
            }

            string prefix;
            ConsoleColor color;

            switch (log.Level)
            {
                case LogLevel.Fatal:
                    prefix = "FTL: ";
                    color = ConsoleColor.Magenta;
                    break;
                case LogLevel.Error:
                    prefix = "ERR: ";
                    color = ConsoleColor.Red;
                    break;
                case LogLevel.Warn:
                    prefix = "WRN: ";
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.Info:
                    prefix = "NFO: ";
                    color = ConsoleColor.Cyan;
                    break;
                    default:
                case LogLevel.Debug:
                    prefix = "DBG: ";
                    color = ConsoleColor.Green;
                    break;
            }

            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(prefix);
            Console.ForegroundColor = previousColor;
            Console.WriteLine(log.Message);
        }
    }
}
