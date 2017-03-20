
namespace DICOMSharp.Logging
{
    /// <summary>
    /// This is a base interface for a logging mechanism for the toolkit.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// This is the method that will be called by any part of the toolkit that would like to log anything.
        /// </summary>
        /// <param name="level">The logging level for the logged message.</param>
        /// <param name="message">The message to be logged.</param>
        void Log(LogLevel level, string message);
    }
}
