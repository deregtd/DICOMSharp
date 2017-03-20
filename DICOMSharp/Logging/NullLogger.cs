
namespace DICOMSharp.Logging
{
    /// <summary>
    /// A helper logger that completely ignores log requests.
    /// </summary>
    public class NullLogger : ILogger
    {
        void ILogger.Log(LogLevel level, string message)
        {
        }
    }
}
