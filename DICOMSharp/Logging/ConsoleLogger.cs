using System;

namespace DICOMSharp.Logging
{
    /// <summary>
    /// A helper logger that simply sends any log messages to the Debug Console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        void ILogger.Log(LogLevel level, string message)
        {
            Console.WriteLine("DICOM# Log (Level: " + level.ToString() + "): " + message);
        }
    }
}
