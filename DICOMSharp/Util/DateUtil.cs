using System;
using System.Globalization;

namespace DICOMSharp.Util
{
    /// <summary>
    /// This static helper class contains helper functions for manipulating DICOM dates.
    /// </summary>
    public static class DateUtil
    {
        /// <summary>
        /// Parses out DICOM date and time fields into a .NET DateTime object.
        /// </summary>
        /// <param name="date">The input string in DICOM date format (DA field -- YYYYmmdd)</param>
        /// <param name="time">The optional input string in DICOM time format (TM field -- HHmmss.ffffff)</param>
        /// <returns>A parsed datetime</returns>
        public static DateTime ConvertDicomDateAndTime(string date, string time = null)
        {
            if (date.Length < 8)
            {
                return DateTime.MinValue;
            }

            try
            {
                int year = int.Parse(date.Substring(0, 4));
                int month = int.Parse(date.Substring(4, 2));
                int day = int.Parse(date.Substring(6, 2));
                int hour = 0, minute = 0, second = 0, ms = 0;

                if (time != null && time.Length >= 4)
                {
                    hour = int.Parse(time.Substring(0, 2));
                    minute = int.Parse(time.Substring(2, 2));

                    if (time.Length >= 6)
                    {
                        second = int.Parse(time.Substring(4, 2));
                    }

                    if (time.Length > 7)
                    {
                        // Get milliseconds out
                        var fracPart = time.Substring(7);
                        if (fracPart.Length > 3)
                        {
                            // Don't care about sub-milliseconds
                            fracPart = fracPart.Substring(0, 3);
                        }
                        while (fracPart.Length < 3)
                        {
                            fracPart += '0';
                        }
                        ms = int.Parse(fracPart);
                    }
                }

                return new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Local);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Converts a unix timestamp to a C# DateTime.
        /// </summary>
        /// <param name="unixTimeStamp">The unix timestamp</param>
        /// <returns>The datetime</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
