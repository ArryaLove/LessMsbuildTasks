using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using dotless.Core.Loggers;
using ILogger = dotless.Core.Loggers.ILogger;

namespace LessCompiler.Tasks
{
    public class LessMsBuildLogger : ILogger
    {
          private readonly LogLevel _level;
          public LessMsBuildLogger(LogLevel level)
        {
            _level = level;
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(string message, params object[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            Log(LogLevel.Error, string.Format(message, args));
        }

        public void Log(LogLevel level, string message)
        {
            if ((int) level < (int) _level)
                return;
            switch (level)
            {
                case LogLevel.Info:
                    Logger.LogMessage(MessageImportance.Normal, message);
                    break;
                case LogLevel.Debug:
                    Logger.LogMessage(MessageImportance.Low, message);
                    break;
                case LogLevel.Warn:
                    Logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Logger.LogError(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Info(string message, params object[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            Log(LogLevel.Info, string.Format(message, args));
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Debug(string message, params object[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            Log(LogLevel.Debug, string.Format(message, args));
        }

        public void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }

        public void Warn(string message, params object[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            Log(LogLevel.Warn, string.Format(message, args));
        }

        public static TaskLoggingHelper Logger { get; set; }
    }
}