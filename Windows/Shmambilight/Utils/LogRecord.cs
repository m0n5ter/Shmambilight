using System;
using NLog;

namespace Shmambilight.Utils
{
    public class LogRecord
    {
        public LogLevel Level { get; }

        public string Message { get; }

        public DateTime Time { get; } = DateTime.Now;

        public Exception Exception { get; }

        public LogRecord(LogLevel level, string message, Exception exception = null)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }
    }
}