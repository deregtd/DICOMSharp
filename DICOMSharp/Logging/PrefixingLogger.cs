using System;

namespace DICOMSharp.Logging
{
    /// <summary>
    /// A logger that wraps another logger and prefixes things
    /// </summary>
    public class PrefixingLogger : ILogger
    {
        private ILogger _wrappedLogger;
        private string _prefix;

        /// <summary>
        /// Create a prefixing logger that encapsulates another logger but prefixes all the log messages
        /// </summary>
        /// <param name="loggerToWrap">The existing ILogger to wrap</param>
        /// <param name="prefix">The string to prefix all log messages with</param>
        public PrefixingLogger(ILogger loggerToWrap, string prefix)
        {
            this._wrappedLogger = loggerToWrap;
            this._prefix = prefix;
        }

        void ILogger.Log(LogLevel level, string message)
        {
            this._wrappedLogger.Log(level, this._prefix + message);
        }
    }
}
