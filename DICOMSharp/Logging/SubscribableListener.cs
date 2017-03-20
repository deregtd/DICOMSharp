
namespace DICOMSharp.Logging
{
    /// <summary>
    /// A helper logger that provides a subscription event for log messages.
    /// </summary>
    public class SubscribableListener : ILogger
    {
        /// <summary>
        /// The delegate event handler for a received log message
        /// </summary>
        /// <param name="level">The logging level for the message</param>
        /// <param name="message">The message text itself</param>
        public delegate void LoggedMessageHandler(LogLevel level, string message);
        /// <summary>
        /// The subscribable event for any logged messages to be passed along to.
        /// </summary>
        public event LoggedMessageHandler MessageLogged;

        /// <summary>
        /// This function initiates a logging action at the specified level with the specified message.
        /// </summary>
        /// <param name="level">The importance level of the logged message.</param>
        /// <param name="message">The message string itself to be logged.</param>
        public void Log(LogLevel level, string message)
        {
            if (MessageLogged != null)
                MessageLogged(level, message);
        }
    }
}
