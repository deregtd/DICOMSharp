using DICOMSharp.Logging;
using PSCommon.Dicom;
using PSCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;
using log4net;

namespace PSWebsite.Utilities
{
    public static class PSUtils
    {
        private static object _lockObj = new object();
        private static PSDatabase _db = null;
        private static ILogger _logger = null;
        private static DicomServer _server = null;
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static ILogger GetLogger()
        {
            lock (_lockObj)
            {
                if (_logger != null)
                {
                    return _logger;
                }

                var listener = new SubscribableListener();
                listener.MessageLogged += Listener_MessageLogged;
                _logger = listener;

                return _logger;
            }
        }

        private static void Listener_MessageLogged(LogLevel level, string message)
        {
            // Output to console
            string logLine = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + level.ToString() + ": " + message;
            Debug.WriteLine(logLine);

            // Log to file
            if (level == LogLevel.Debug)
            {
                log.Debug(message);
            }
            else if (level == LogLevel.Error)
            {
                log.Error(message);
            }
            else if (level == LogLevel.Info)
            {
                log.Info(message);
            }
            else if (level == LogLevel.Warning)
            {
                log.Warn(message);
            }
        }

        public static PSDatabase GetDb()
        {
            lock (_lockObj)
            {
                if (_db != null)
                {
                    return _db;
                }

                var serverSettings = (NameValueCollection)ConfigurationManager.GetSection("serverSettings");
                if (serverSettings == null)
                {
                    throw new Exception("serverSettings field missing -- ensure your WebUser.config is setup properly");
                }

                var dbSettings = new DatabaseSettings();

                if (!Enum.TryParse(serverSettings["DatabaseType"], out dbSettings.DBType))
                {
                    throw new ArgumentException("Unknown DatabaseType: " + serverSettings["DatabaseType"]);
                }

                dbSettings.SQLitePath = serverSettings["DatabaseSqlitePath"] ?? "";
                if (dbSettings.SQLitePath.StartsWith(@"~/"))
                {
                    dbSettings.SQLitePath = HttpContext.Current.Server.MapPath(dbSettings.SQLitePath);
                }

                if (dbSettings.DBType != PSDatabase.DBType.SQLite)
                {
                    dbSettings.Hostname = serverSettings["DatabaseServer"];
                    dbSettings.Username = serverSettings["DatabaseUsername"];
                    dbSettings.DBName = serverSettings["DatabaseName"];
                }

                dbSettings.Password = serverSettings["DatabasePassword"] ?? "";

                var db = new PSDatabase(GetLogger(), HttpContext.Current.Server.MapPath(@"~/"));
                db.UseSettings(dbSettings);

                // If we get this far without an exception then we connected
                _db = db;
                return _db;
            }
        }

        public static DicomServer GetDicomServer()
        {
            lock (_lockObj)
            {
                if (_server != null)
                {
                    return _server;
                }

                _server = new DicomServer(GetLogger(), GetDb(), HttpContext.Current.Server.MapPath(@"~/"));
                return _server;
            }
        }

        public static string GetParsedFilePath(string filePath)
        {
            if (filePath.StartsWith("~") && HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                return HttpContext.Current.Server.MapPath(filePath);
            }

            return filePath;
        }
    }
}