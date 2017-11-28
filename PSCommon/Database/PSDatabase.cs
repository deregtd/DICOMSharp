using DICOMSharp.Data;
using DICOMSharp.Data.Dictionary;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Logging;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.QueryRetrieve;
using DICOMSharp.Util;
using MySql.Data.MySqlClient;
using PSCommon.Models;
using PSCommon.Models.StudyBrowser;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace PSCommon.Utilities
{
    public class DatabaseSettings
    {
        public PSDatabase.DBType DBType;
        public string Hostname, SQLitePath, Username, Password, DBName;
    }

    public class PSDatabase
    {
        public const string Option_SchemaVersion = "SchemaVersion";

        private string _tildePathReplace = null;

        private struct stFieldSource
        {
            public uint tag;
            public string fieldSource;
            public Type type;
            public bool ranged;

            public stFieldSource(uint tag, string fieldSource, Type type, bool ranged)
            { this.tag = tag; this.fieldSource = fieldSource; this.type = type; this.ranged = ranged; }
        }

        private static Dictionary<uint, stFieldSource> fieldSources = new Dictionary<uint, stFieldSource>();
        static PSDatabase()
        {
            List<stFieldSource> sourceList = new List<stFieldSource>();

            sourceList.Add(new stFieldSource(DICOMTags.PatientID, "Patients.PatID", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.PatientName, "Patients.PatName", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.PatientBirthDate, "Patients.PatBirthDate", typeof(int), false));
            sourceList.Add(new stFieldSource(DICOMTags.PatientSex, "Patients.PatSex", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfPatientRelatedStudies, "Patients.NumStudies", typeof(int), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfPatientRelatedSeries, "Patients.NumSeries", typeof(int), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfPatientRelatedInstances, "Patients.NumImages", typeof(int), false));

            sourceList.Add(new stFieldSource(DICOMTags.StudyInstanceUID, "Studies.StuInstID", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.StudyID, "Studies.StuID", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.StudyDate, "Studies.StuDate", typeof(int), true));
            sourceList.Add(new stFieldSource(DICOMTags.StudyTime, "Studies.StuTime", typeof(float), true));
            sourceList.Add(new stFieldSource(DICOMTags.AccessionNumber, "Studies.AccessionNum", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.Modality, "Studies.Modality", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.ReferringPhysicianName, "Studies.RefPhysician", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.StudyDescription, "Studies.StuDesc", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.InstitutionalDepartmentName, "Studies.DeptName", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfStudyRelatedSeries, "Studies.NumSeries", typeof(int), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfStudyRelatedInstances, "Studies.NumImages", typeof(int), false));

            sourceList.Add(new stFieldSource(DICOMTags.SeriesInstanceUID, "Series.SerInstID", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.SeriesDate, "Series.SerDate", typeof(int), true));
            sourceList.Add(new stFieldSource(DICOMTags.SeriesTime, "Series.SerTime", typeof(float), true));
            sourceList.Add(new stFieldSource(DICOMTags.SeriesNumber, "Series.SerNum", typeof(int), false));
            //sourceList.Add(new stFieldSource(DICOMTags.Modality, "Series.Modality", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.SeriesDescription, "Series.SerDesc", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.BodyPartExamined, "Series.BodyPart", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.NumberOfSeriesRelatedInstances, "Series.NumImages", typeof(int), false));

            sourceList.Add(new stFieldSource(DICOMTags.SOPInstanceUID, "Images.ImaInstID", typeof(string), false));
            sourceList.Add(new stFieldSource(DICOMTags.InstanceNumber, "Images.ImaNum", typeof(int), false));
            sourceList.Add(new stFieldSource(DICOMTags.SourceApplicationEntityTitle, "Images.SendingAE", typeof(string), false));

            foreach (stFieldSource field in sourceList)
                fieldSources.Add(field.tag, field);
        }


        public PSDatabase(ILogger logger, string tildePathReplace = null)
        {
            this._logger = logger;
            this._tildePathReplace = tildePathReplace;

            TotalSizeKB = 0;
            PruneDBSizeMB = 0;
            IsSetup = false;
        }

        public void UseSettings(DatabaseSettings settings)
        {
            Close();

            _databaseType = settings.DBType;

            if (_databaseType == DBType.SQLite)
            {
                if (string.IsNullOrWhiteSpace(settings.SQLitePath))
                {
                    throw new ArgumentException("SQLite Path cannot be blank!");
                }

                _connection = new SQLiteConnection("DataSource=" + settings.SQLitePath + ".db;Password=" + settings.Password + ";");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(settings.Hostname) ||
                    string.IsNullOrWhiteSpace(settings.Username) ||
                    string.IsNullOrWhiteSpace(settings.Password) ||
                    string.IsNullOrWhiteSpace(settings.DBName))
                {
                    throw new ArgumentException("Database access settings cannot be blank!");
                }

                _databaseName = settings.DBName;

                if (_databaseType == DBType.MSSQL)
                {
                    _connection = new SqlConnection("Data Source=" + settings.Hostname + ";User Id=" + settings.Username + ";Password=" + settings.Password + ";MultipleActiveResultSets=true;");
                }
                else if (_databaseType == DBType.MySQL)
                {
                    _connection = new MySqlConnection("Server=" + settings.Hostname + ";Uid=" + settings.Username + ";Pwd=" + settings.Password + ";");
                }
                else
                {
                    throw new ArgumentException("Unsupported Database Type");
                }
            }

            CheckConnection();
        }

        private DateTime _lastCheckedConnection = DateTime.MinValue;

        /// <summary>
        /// Make sure that the database is still open and connected to the right database.
        /// Creates the database as needed.
        /// </summary>
        private void CheckConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Connection not opened yet.  You must call UseSettings before attempting to use the connection.");
            }

            // Check the connection every 5 minutes
            if (_connection.State == System.Data.ConnectionState.Open && DateTime.Now - _lastCheckedConnection > new TimeSpan(0, 5, 0))
            {
                try {
                    // Try pinging the connection to see if it craps out
                    DbCommand cmd = _connection.CreateCommand();
                    cmd.CommandText = "SELECT 4";
                    cmd.ExecuteScalar();
                }
                catch
                {
                }

                _lastCheckedConnection = DateTime.Now;
            }

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                // Will throw an exception if the connection doesn't open/work
                _connection.Open();

                CheckSetup();
            }
        }

        private void CheckSetup()
        {
            IsSetup = false;

            if (!IsConnected)
            {
                return;
            }

            if (_databaseType != DBType.SQLite)
            {
                // Make sure that the database exists
                lock (_connection)
                {
                    try
                    {
                        ExecuteNonQuery("USE " + _databaseName);
                    }
                    catch
                    {
                        // Database can't be used, so let's try creating it
                        ExecuteNonQuery("CREATE DATABASE " + _databaseName);

                        // See if we can switch to it now
                        ExecuteNonQuery("USE " + _databaseName);
                    }
                }
            }

            var psCommonAssembly = Assembly.GetAssembly(typeof(PSUser));

            var schemaVersion = GetOptionInt(Option_SchemaVersion, 0, 1);

            if (schemaVersion == 0)
            {
                // No tables!
                string createSchema = new StreamReader(psCommonAssembly.GetManifestResourceStream("PSCommon.Resources.CreateBackendSchema.sql")).ReadToEnd();
                createSchema += new StreamReader(psCommonAssembly.GetManifestResourceStream("PSCommon.Resources.CreateImagesSchema.sql")).ReadToEnd();

                _logger.Log(LogLevel.Warning, "Running backend+images create scripts...");
                if (!RunBatchScript(createSchema))
                {
                    _logger.Log(LogLevel.Error, "Error running backend+images create schema");
                    return;
                }
            }

            if (schemaVersion == 1)
            {
                // Upgrade adds TaskQueue table
                string upgradeSchema = new StreamReader(psCommonAssembly.GetManifestResourceStream("PSCommon.Resources.Upgrade1to2.sql")).ReadToEnd();

                _logger.Log(LogLevel.Warning, "Running upgrade from 1->2...");
                if (!RunBatchScript(upgradeSchema))
                {
                    _logger.Log(LogLevel.Error, "Error running upgrade schema");
                    return;
                }

                SetOption(Option_SchemaVersion, 2);
                schemaVersion = 2;
            }

            if (schemaVersion == 2)
            {
                // Upgrade adds Flags column to the Entities table
                string upgradeSchema = new StreamReader(psCommonAssembly.GetManifestResourceStream("PSCommon.Resources.Upgrade2to3.sql")).ReadToEnd();

                _logger.Log(LogLevel.Warning, "Running upgrade from 2->3...");
                if (!RunBatchScript(upgradeSchema))
                {
                    _logger.Log(LogLevel.Error, "Error running upgrade schema");
                    return;
                }

                SetOption(Option_SchemaVersion, 3);
                schemaVersion = 3;
            }

            // Done!
            IsSetup = true;

            TotalSizeKB = CalculateDBSizeKB();

            return;
        }

        private bool DoesTableExist(string tableName)
        {
            try
            {
                lock (_connection)
                {
                    // TODO: Figure out a better way to do this
                    using (DbDataReader reader = ExecuteReader("SELECT * FROM " + tableName))
                    {
                        reader.Close();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Forcibly close the database connection
        /// </summary>
        public void Close()
        {
            IsSetup = false;
            TotalSizeKB = 0;

            if (_connection == null)
            {
                return;
            }

            if (_connection.State != System.Data.ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// Does a database-engine-specific escaping of strings for queries.
        /// </summary>
        /// <param name="str">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        private string Escape(string str)
        {
            if (_databaseType == DBType.MSSQL)
                return str.Replace("'", "''");
            else if (_databaseType == DBType.MySQL)
                return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");
            else if (_databaseType == DBType.SQLite)
                return str.Replace("'", "''");

            throw new NotSupportedException("Unsupported Database Type");
        }

        /// <summary>
        /// After an identity insert, call this to get the ID of your insert.
        /// </summary>
        /// <returns>The identity value.</returns>
        private int GetIdentityInsertID()
        {
            lock (_connection)
            {
                if (_databaseType == DBType.MSSQL)
                    return ExecuteNumericScalar("SELECT CAST(@@IDENTITY AS int) AS 'Identity'");
                else if (_databaseType == DBType.MySQL)
                    return ExecuteNumericScalar("SELECT LAST_INSERT_ID() AS 'Identity'");
                else if (_databaseType == DBType.SQLite)
                    return ExecuteNumericScalar("Select last_insert_rowid() as 'Identity'");
                else
                    throw new NotSupportedException("Unsupported Database Type");
            }
        }

        private string GetNowString()
        {
            if (_databaseType == DBType.MSSQL)
                return "{ fn NOW() }";
            else if (_databaseType == DBType.MySQL)
                return "NOW()";
            else if (_databaseType == DBType.SQLite)
                return "strftime('%s','now')";

            throw new NotSupportedException("Unsupported Database Type");
        }

        // Returns rows affected
        private int ExecuteNonQuery(string query, int retryCount = 5)
        {
            return ExecuteSomethingWithRetries(query, (cmd) => cmd.ExecuteNonQuery(), retryCount);
        }

        private DbDataReader ExecuteReader(string query, int retryCount = 5)
        {
            return ExecuteSomethingWithRetries(query, (cmd) => cmd.ExecuteReader(), retryCount);
        }

        private object ExecuteScalar(string query, int retryCount = 5)
        {
            return ExecuteSomethingWithRetries(query, (cmd) => cmd.ExecuteScalar(), retryCount);
        }

        private int ExecuteNumericScalar(string query, int def = -1)
        {
            object scalarRet = ExecuteScalar(query);
            return (scalarRet != null) ? Convert.ToInt32(scalarRet) : def;
        }

        private T ExecuteSomethingWithRetries<T>(string query, Func<DbCommand, T> lambda, int retryCount = 5)
        {
            while (true)
            {
                CheckConnection();

                DbCommand cmd = _connection.CreateCommand();
                cmd.CommandText = query;
                try
                {
                    return lambda(cmd);
                }
                catch
                {
                    if (retryCount <= 0)
                    {
                        throw;
                    }

                    Thread.Sleep(1000);
                    retryCount--;
                }
            }
        }

        //----------Schema creation guts----------

        public string ReplaceCreateSchema(string createSchema)
        {
            switch (_databaseType)
            {
                case DBType.MSSQL:
                    createSchema = createSchema.Replace("$$1$$", "IDENTITY(1,1)");
                    createSchema = createSchema.Replace("$$2$$", "");
                    break;
                case DBType.MySQL:
                    createSchema = createSchema.Replace("$$1$$", "AUTO_INCREMENT PRIMARY KEY");
                    createSchema = createSchema.Replace("$$2$$", "ENGINE=myisam");
                    break;
                case DBType.SQLite:
                    createSchema = createSchema.Replace("$$1$$", "PRIMARY KEY AUTOINCREMENT");
                    createSchema = createSchema.Replace("$$2$$", "");
                    createSchema = createSchema.Replace(" int", " INTEGER");
                    createSchema = createSchema.Replace(" datetime", " INTEGER");
                    break;
                default:
                    throw new NotSupportedException("Unsupported Database Type");
            }
            
            if (_databaseType == PSDatabase.DBType.MySQL || _databaseType == DBType.SQLite)
                createSchema = createSchema.Replace("$$3$$", "DROP TABLE IF EXISTS");
            else
                createSchema = Regex.Replace(createSchema, "\\$\\$3\\$\\$ ([a-zA-Z]*)", "IF OBJECT_ID('dbo.$1', 'U') IS NOT NULL DROP TABLE $1");

            return createSchema;
        }

        public bool WipeDB()
        {
            IsSetup = false;

            _logger.Log(LogLevel.Warning, "Nuking tables...");

            string query;
            switch (_databaseType)
            {
                case DBType.MSSQL:
                    query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.Tables";
                    break;
                case DBType.MySQL:
                    query = "SHOW TABLES";
                    break;
                case DBType.SQLite:
                    query = "SELECT name FROM sqlite_master WHERE type = 'table'";
                    break;
                default:
                    throw new NotSupportedException("Unsupported Database Type");
            }

            var tableNames = new List<string>();
            using (DbDataReader reader = ExecuteReader(query))
            {
                while (reader.Read())
                {
                    tableNames.Add(reader[0] as string);
                }
                reader.Close();
            }

            foreach (var tableName in tableNames)
            {
                ExecuteNonQuery("DROP TABLE " + tableName);
            }

            _logger.Log(LogLevel.Warning, "Tables nuked. Setting back up...");

            CheckSetup();

            //now create images db too
            return true;
        }

        public bool RecreateImagesDB()
        {
            if (!IsSetup)
                return false;

            var psCommonAssembly = Assembly.GetAssembly(typeof(PSUser));
            string createSchema = new StreamReader(psCommonAssembly.GetManifestResourceStream("PSCommon.Resources.CreateImagesSchema.sql")).ReadToEnd();
            _logger.Log(LogLevel.Warning, "Running image schema create scripts...");
            if (!RunBatchScript(createSchema))
            {
                _logger.Log(LogLevel.Error, "Error running image create schema");
                return false;
            }

            _logger.Log(LogLevel.Warning, "Images recreate script complete -- images database now empty!");

            CheckSetup();
            return true;
        }

        private bool RunBatchScript(string script)
        {
            script = ReplaceCreateSchema(script);

            try
            {
                lock (_connection)
                {
                    foreach (string statement in script.Split(new string[] { "\r\nGO\r\n", "\nGO\n", ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        ExecuteNonQuery(statement);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, "Error running batch script: " + e.ToString());
                return false;
            }

            return true;
        }

        //---------- Options -----------

        public string GetOption(string key, string defaultVal = null, int retries = 5)
        {
            string ret = defaultVal;
            lock (_connection)
            {
                try
                {
                    using (DbDataReader reader = ExecuteReader("SELECT Value FROM Options WHERE KeyName = '" + Escape(key) + "'", retries))
                    {
                        if (reader.Read())
                            ret = reader[0] as string;
                        reader.Close();
                    }
                } catch {
                    return null;
                }
            }

            return ret;
        }

        public int GetOptionInt(string key, int defaultVal = 0, int retries = 5)
        {
            string val = GetOption(key, defaultVal.ToString(), retries);
            if (val == null)
            {
                return defaultVal;
            }

            int outInt = defaultVal;
            int.TryParse(val, out outInt);
            return outInt;
        }

        public bool GetOptionBool(string key, bool defaultVal = false)
        {
            string val = GetOption(key);
            if (val == null)
            {
                return defaultVal;
            }

            bool outBool = defaultVal;
            bool.TryParse(val, out outBool);
            return outBool;
        }

        public void SetOption(string key, string val)
        {
            lock (_connection)
            {
                ExecuteNonQuery("DELETE FROM Options WHERE KeyName = '" + Escape(key) + "'");
                ExecuteNonQuery("INSERT INTO Options (KeyName, Value) VALUES ('" + Escape(key) + "', '" + Escape(val) + "')");
            }
        }

        public void SetOption(string key, int val)
        {
            SetOption(key, val.ToString());
        }

        public void SetOption(string key, bool val)
        {
            SetOption(key, val.ToString());
        }

        //---------- Users -----------

        public PSUser GetUser(string username, bool populatePassword)
        {
            if (!IsSetup)
                return null;

            PSUser ui = null;
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Users WHERE Username = '" + Escape(username) + "'"))
                {
                    if (reader.Read())
                        ui = new PSUser(reader, populatePassword);
                    reader.Close();
                }
            }

            return ui;
        }

        public List<PSUser> GetUsers(bool populatePassword)
        {
            if (!IsSetup)
                return new List<PSUser>();

            List<PSUser> users = new List<PSUser>();
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Users"))
                {
                    while (reader.Read())
                        users.Add(new PSUser(reader, populatePassword));
                    reader.Close();
                }
            }

            return users;
        }

        public void UserDelete(string username)
        {
            lock (_connection)
            {
                ExecuteNonQuery("DELETE FROM Users WHERE Username = '" + Escape(username) + "'");
            }
        }

        public void UserInsert(PSUser newUser)
        {
            lock (_connection)
            {
                // Check to make sure we're not trying to insert a user on top of another user
                var testUser = GetUser(newUser.username, false);
                if (testUser != null)
                {
                    throw new System.Exception("Username already exists");
                }

                ExecuteNonQuery("INSERT INTO Users (Username, Password, Realname, Access) VALUES ('" +
                    Escape(newUser.username) + "', '" + Escape(newUser.password) + "', '" +
                    Escape(newUser.realname) + "', " + ((int) newUser.access).ToString() + ")");
            }
        }

        public void UserUpdate(string oldusername, PSUser nuser)
        {
            lock (_connection)
            {
                // Check to make sure we're not trying to rename a user on top of another user
                if (oldusername != nuser.username)
                {
                    var testUser = GetUser(nuser.username, false);
                    if (testUser != null)
                    {
                        throw new System.Exception("Username already exists");
                    }
                }

                ExecuteNonQuery("UPDATE Users SET Username = '" + Escape(nuser.username) + "', Password = '" +
                    Escape(nuser.password) + "', Realname = '" + Escape(nuser.realname) + "', Access = " +
                    ((int) nuser.access).ToString() + " WHERE Username = '" + Escape(oldusername) + "'");

                //rename logs too if needed...
                if (oldusername != nuser.username)
                {
                    ExecuteNonQuery("UPDATE Logs SET Username = '" + Escape(nuser.username) +
                        "' WHERE Username = '" + Escape(oldusername) + "'");
                }
            }
        }

        public List<PSLogEntry> GetLogsForUser(string username)
        {
            List<PSLogEntry> logs = new List<PSLogEntry>();
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Logs WHERE Username = '" + Escape(username) + "'"))
                {
                    if (reader.Read())
                        logs.Add(new PSLogEntry(reader));
                    reader.Close();
                }
            }

            return logs;
        }


        //---------- Sessions -----------

        public void SessionDelete(string sessionId)
        {
            lock (_connection)
            {
                ExecuteNonQuery("DELETE FROM Sessions WHERE SessionId = '" + Escape(sessionId) + "'");
            }
        }

        public void AttachUsernameToSession(string sessionId, string username)
        {
            lock (_connection)
            {
                ExecuteNonQuery("INSERT INTO Sessions (SessionId, Username) VALUES ('" +
                    Escape(sessionId) + "', '" + Escape(username) + "')");
            }
        }

        public string GetUsernameForSession(string sessionId)
        {
            if (!IsSetup)
                return null;

            string ret = null;
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Sessions WHERE SessionId = '" + Escape(sessionId) + "'"))
                {
                    if (reader.Read())
                    {
                        ret = (string) reader["Username"];
                    }
                    reader.Close();
                }
            }

            return ret;
        }

        //---------- Entities -----------

        public PSEntity GetEntity(string aeTitle)
        {
            if (!IsSetup)
                return null;

            PSEntity entity = null;
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Entities WHERE AE = '" + Escape(aeTitle.Trim()) + "'"))
                {
                    if (reader.Read())
                    {
                        entity = new PSEntity(reader);
                    }
                    reader.Close();
                }
            }

            return entity;
        }

        public List<PSEntity> GetEntities()
        {
            if (!IsSetup)
            {
                return new List<PSEntity>();
            }

            var entities = new List<PSEntity>();
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT * FROM Entities"))
                {
                    while (reader.Read())
                    {
                        entities.Add(new PSEntity(reader));
                    }
                    reader.Close();
                }
            }

            return entities;
        }

        public void EntityDelete(string aeTitle)
        {
            lock (_connection)
            {
                ExecuteNonQuery("DELETE FROM Entities WHERE AE = '" + Escape(aeTitle) + "'");
            }
        }

        public void EntityInsert(PSEntity newAE)
        {
            lock (_connection)
            {
                ExecuteNonQuery("INSERT INTO Entities (AE, Address, Port, Flags, Comment) VALUES ('" + Escape(newAE.Title) +
                    "', '" + Escape(newAE.Address) + "', " + newAE.Port.ToString() + ", " + ((uint) newAE.Flags).ToString() +
                    ", '" + Escape(newAE.Comment) + "')");
            }
        }


        // --------- Other -------------

        private long CalculateDBSizeKB()
        {
            if (!IsSetup)
                return 0;

            lock (_connection)
            {
                object ret = ExecuteScalar("SELECT SUM(PatSizeKB) AS Expr1 FROM Patients");
                if (ret is DBNull) return 0;
                return (long)(double)ret;
            }
        }

        public bool PersistImage(DICOMData data, string diskPath, string dbPath)
        {
            try
            {
                //required tags
                FileInfo fi = new FileInfo(diskPath);
                if (!fi.Exists)
                {
                    _logger.Log(LogLevel.Error, "File's path doesn't exist...");
                    return false;
                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                List<float> watchPoints = new List<float>();

                int fileSizeKB = (int)(fi.Length / 1024);

                string instanceUID = GetRequiredTag(data, DICOMTags.SOPInstanceUID);
                string seriesUID = GetRequiredTag(data, DICOMTags.SeriesInstanceUID);
                string studyUID = GetRequiredTag(data, DICOMTags.StudyInstanceUID);
                string sopClassUID = GetRequiredTag(data, DICOMTags.SOPClassUID);
                string transferSyntaxUID = GetRequiredTag(data, DICOMTags.TransferSyntaxUID);

                string stuDate = GetDate(data, DICOMTags.StudyDate, true);
                string stuTime = GetTime(data, DICOMTags.StudyTime, true);

                //optional tags
                string patName = GetTagOrValue(data, DICOMTags.PatientName, "[No Name]", 64);
                string patID = GetTagOrValue(data, DICOMTags.PatientID, "[No ID]", 64);
                string patSex = GetTagOrValue(data, DICOMTags.PatientSex, "", 16);
                string modality = GetTagOrValue(data, DICOMTags.Modality, "UN", 2);

                int seriesNum = 0; int.TryParse(GetTagOrValue(data, DICOMTags.SeriesNumber, "0"), out seriesNum);
                int instanceNum = 0; int.TryParse(GetTagOrValue(data, DICOMTags.InstanceNumber, "0"), out instanceNum);

                string patBirthDate = GetDate(data, DICOMTags.PatientBirthDate);
                string sendingAE = GetTagOrValue(data, DICOMTags.SourceApplicationEntityTitle, "", 16);
                string bodyPartExa = GetTagOrValue(data, DICOMTags.BodyPartExamined, "", 64);
                string seriesDesc = GetTagOrValue(data, DICOMTags.SeriesDescription, "", 64);
                string studyDesc = GetTagOrValue(data, DICOMTags.StudyDescription, "", 64);
                string studyID = GetTagOrValue(data, DICOMTags.StudyID, "", 16);
                string accessionNum = GetTagOrValue(data, DICOMTags.AccessionNumber, "", 16);
                string refPhysician = GetTagOrValue(data, DICOMTags.ReferringPhysicianName, "", 64);
                string deptName = GetTagOrValue(data, DICOMTags.InstitutionalDepartmentName, "", 64);
                string seriesDate = GetDate(data, DICOMTags.SeriesDate);
                string seriesTime = GetTime(data, DICOMTags.SeriesTime);

                // TODO: Make this into an exclusive transaction
                lock (_connection)
                {
                    //Get info about the existing image in the DB, if it's there...
                    int oldFileSizeKB = -1, oldIntSerID = -1, oldIntStuID = -1, oldIntPatID = -1;
                    using (DbDataReader reader = ExecuteReader("SELECT i.FileSizeKB, i.IntSerID, se.IntStuID, st.IntPatID FROM Images i LEFT JOIN Series se ON i.IntSerID = se.IntSerID LEFT JOIN Studies st ON se.IntStuID = st.IntStuID WHERE ImaInstID = '" + Escape(instanceUID) + "'"))
                    {
                        if (reader.Read())
                        {
                            oldFileSizeKB = Convert.ToInt32(reader["FileSizeKB"]);
                            oldIntSerID = Convert.ToInt32(reader["IntSerID"]);
                            oldIntStuID = Convert.ToInt32(reader["IntStuID"]);
                            oldIntPatID = Convert.ToInt32(reader["IntPatID"]);
                        }
                        reader.Close();
                    }

                    //Check to see if the pat/stu/ser for the new image exist or not
                    int intPatID = ExecuteNumericScalar("SELECT IntPatID FROM Patients WHERE PatID = '"
                        + Escape(patID) + "' AND PatName = '" + Escape(patName) + "' AND PatBirthDate = '" + Escape(patBirthDate) + "'");

                    int intStuID = ExecuteNumericScalar("SELECT IntStuID FROM Studies WHERE StuInstID = '" + Escape(studyUID) + "'");

                    int intSerID = ExecuteNumericScalar("SELECT IntSerID FROM Series WHERE SerInstID = '" + Escape(seriesUID) + "'");

                    watchPoints.Add((float)(1000.0 * watch.ElapsedTicks / Stopwatch.Frequency));

                    if (oldFileSizeKB > -1)
                    {
                        //exists -- delete old instance
                        ExecuteNonQuery("DELETE FROM Images WHERE ImaInstID = '" + Escape(instanceUID) + "'");
                        TotalSizeKB -= oldFileSizeKB;
                    }

                    bool makePat = (intPatID == -1), makeStu = (intStuID == -1), makeSer = (intSerID == -1);
                    if (makePat)
                    {
                        //should always be making a study and series too..
                        Debug.Assert(makeStu && makeSer);

                        //make a patient
                        ExecuteNonQuery("INSERT INTO Patients (PatID, PatName, PatBirthDate, PatSex, NumStudies, " +
                            "NumSeries, NumImages, PatSizeKB, LastUsedTime) VALUES ('" + Escape(patID) + "', '" +
                            Escape(patName) + "', '" + Escape(patBirthDate) + "', '" + Escape(patSex) + "', 1, 1, 1, " +
                            fileSizeKB.ToString() + ", " + GetNowString() + ")");
                        intPatID = GetIdentityInsertID();
                    }
                    if (makeStu)
                    {
                        //should always be making a series too..
                        Debug.Assert(makeSer);

                        //make a study
                        ExecuteNonQuery("INSERT INTO Studies (StuInstID, IntPatID, PatID, NumSeries, NumImages, StuSizeKB, " +
                            "StuID, StuDate, StuTime, AccessionNum, Modality, RefPhysician, StuDesc, DeptName, LastUsedTime" +
                            ") VALUES ('" + Escape(studyUID) + "', " + intPatID.ToString() + ", '" +
                            Escape(patID) + "', 1, 1, " + fileSizeKB.ToString() + ", '" + Escape(studyID) + "', '" +
                            Escape(stuDate) + "', '" + Escape(stuTime) + "', '" + Escape(accessionNum) + "', '" + Escape(modality) + "', '" +
                            Escape(refPhysician) + "', '" + Escape(studyDesc) + "', '" + Escape(deptName) + "', " +
                            GetNowString() + ")");
                        intStuID = GetIdentityInsertID();
                    }
                    if (makeSer)
                    {
                        //make a series
                        ExecuteNonQuery("INSERT INTO Series (SerInstID, IntStuID, NumImages, SerSizeKB, SerDate, " +
                            "SerTime, SerNum, Modality, SerDesc, BodyPart, LastUsedTime) VALUES ('" +
                            Escape(seriesUID) + "', " + intStuID.ToString() + ", 1, " + fileSizeKB.ToString() + ", '" +
                            Escape(seriesDate) + "', '" + Escape(seriesTime) + "', " + seriesNum.ToString() + ", '" +
                            Escape(modality) + "', '" + Escape(seriesDesc) + "', '" + Escape(bodyPartExa) + "', " +
                            GetNowString() + ")");
                        intSerID = GetIdentityInsertID();
                    }

                    //insert image
                    ExecuteNonQuery("INSERT INTO Images (ImaInstID, IntSerID, SOPClassID, TransferSyntaxID, ImaNum, FileSizeKB, Path, " +
                        "SendingAE, LastUsedTime) VALUES ('" + Escape(instanceUID) + "', " + intSerID.ToString() +
                        ", '" + Escape(sopClassUID) + "', '" + Escape(transferSyntaxUID) + "', " + instanceNum.ToString() + ", " + fileSizeKB.ToString() + ", '" +
                        Escape(dbPath) + "', '" + Escape(sendingAE) + "', " + GetNowString() + ")");
                    TotalSizeKB += fileSizeKB;

                    watchPoints.Add((float)(1000.0 * watch.ElapsedTicks / Stopwatch.Frequency));

                    //If any of the demographics changed, we need to update the old ones to not end up with orphans
                    if (oldIntSerID > -1 && intSerID != oldIntSerID)
                        UpdateSeriesStats(oldIntSerID);

                    if (oldIntStuID > -1 && intStuID != oldIntStuID)
                        UpdateStudyStats(oldIntStuID);

                    if (oldIntPatID > -1 && intPatID != oldIntPatID)
                        UpdatePatientStats(oldIntPatID);

                    //We can optimize this if we need to, but I suspect these queries won't be the limiting factor on
                    //      inserts... and I like the safety of recalcing it instead of using inc/decs...
                    if (!makeSer)
                        UpdateSeriesStats(intSerID);
                    if (!makeStu)
                        UpdateStudyStats(intStuID);
                    if (!makePat)
                        UpdatePatientStats(intPatID);

                    watchPoints.Add((float)(1000.0 * watch.ElapsedTicks / Stopwatch.Frequency));
                    watch.Stop();

                    //logger.Log(LogLevel.Info, "Watchpoints: " + String.Join(",", watchPoints.ToArray()));

                    CheckPrune();
                }

                return true;
            }
            catch (ArgumentNullException e)
            {
                _logger.Log(LogLevel.Error, "Instance missing required tag: " + e.ParamName);
                return false;
            }
        }

        private void UpdatePatientStats(int intPatID)
        {
            lock (_connection)
            {
                using (var reader = ExecuteReader("SELECT COALESCE(SUM(StuSizeKB),0), COALESCE(SUM(NumImages),0), COALESCE(SUM(NumSeries),0), COUNT(IntStuID) FROM Studies WHERE IntPatID = " + intPatID.ToString()))
                {
                    reader.Read();

                    long patSizeKB = Convert.ToInt64(reader[0]);
                    int numImages = Convert.ToInt32(reader[1]);
                    int numSeries = Convert.ToInt32(reader[2]);
                    int numStudies = Convert.ToInt32(reader[3]);

                    if (numImages > 0)
                    {
                        ExecuteNonQuery("UPDATE Patients SET PatSizeKB = " + patSizeKB + ", NumImages = " + numImages + ", NumSeries = " + numSeries +
                            ", NumStudies = " + numStudies + ", LastUsedTime = " + GetNowString() + " WHERE IntPatID = " + intPatID.ToString());
                    }
                    else
                    {
                        ExecuteNonQuery("DELETE FROM Patients WHERE IntPatID = " + intPatID.ToString());
                    }
                }
            }
        }

        private void UpdateStudyStats(int intStuID)
        {
            lock (_connection)
            {
                using (var reader = ExecuteReader("SELECT COALESCE(SUM(SerSizeKB),0), COALESCE(SUM(NumImages),0), COUNT(IntSerID) FROM Series WHERE IntStuID = " + intStuID.ToString()))
                {
                    reader.Read();

                    int stuSizeKB = Convert.ToInt32(reader[0]);
                    int numImages = Convert.ToInt32(reader[1]);
                    int numSeries = Convert.ToInt32(reader[2]);

                    if (numImages > 0)
                    {
                        ExecuteNonQuery("UPDATE Studies SET StuSizeKB = " + stuSizeKB + ", NumImages = " + numImages + ", NumSeries = " + numSeries +
                            ", LastUsedTime = " + GetNowString() + " WHERE IntStuID = " + intStuID.ToString());
                    }
                    else
                    {
                        ExecuteNonQuery("DELETE FROM Studies WHERE IntStuID = " + intStuID.ToString());
                    }
                }
            }
        }

        private void UpdateSeriesStats(int intSerID)
        {
            lock (_connection)
            {
                using (var reader = ExecuteReader("SELECT COALESCE(SUM(FileSizeKB),0), COUNT(ImaInstID) FROM Images WHERE IntSerID = " + intSerID.ToString()))
                {
                    reader.Read();

                    int serSizeKB = Convert.ToInt32(reader[0]);
                    int numImages = Convert.ToInt32(reader[1]);

                    if (numImages > 0)
                    {
                        ExecuteNonQuery("UPDATE Series SET SerSizeKB = " + serSizeKB + ", NumImages = " + numImages + ", LastUsedTime = " + GetNowString() + " WHERE IntSerID = " + intSerID.ToString());
                    }
                    else
                    {
                        ExecuteNonQuery("DELETE FROM Series WHERE IntSerID = " + intSerID.ToString());
                    }
                }
            }
        }

        private void CheckPrune()
        {
            if (PruneDBSizeMB > 0 && TotalSizeKB > (long)PruneDBSizeMB * 1024)
            {
                _logger.Log(LogLevel.Warning, "Database now exceeds maximum size!  Pruning oldest images!");

                lock (_connection)
                {
                    using (DbDataReader reader = ExecuteReader("SELECT Images.* ORDER BY i.LastUsedTime"))
                    {
                        while (reader.Read() && TotalSizeKB > (long)PruneDBSizeMB * 1024)
                        {
                            var image = new PSImage(reader);
                            File.Delete(FixImagePath(image.Path));
                            DeleteImage(image);
                        }
                        reader.Close();
                    }
                }
            }
        }

        public void DeleteImage(PSImage image)
        {
            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader("SELECT s.IntStuID, t.IntPatID FROM Series s LEFT JOIN Studies t ON s.IntStuID = t.IntStuID WHERE s.IntSerId = " + image.IntSerID))
                {
                    if (!reader.Read())
                    {
                        // No row found
                        return;
                    }

                    int intStuId = Convert.ToInt32(reader["IntStuID"]);
                    int intPatId = Convert.ToInt32(reader["IntPatID"]);

                    ExecuteNonQuery("DELETE FROM Images WHERE ImaInstID = '" + Escape(image.ImaInstID) + "'");

                    TotalSizeKB -= image.FileSizeKB;

                    UpdateSeriesStats((int)image.IntSerID);
                    UpdateStudyStats(intStuId);
                    UpdatePatientStats(intPatId);
                }
            }
        }

        public string GetDicomDate(DateTime dt)
        {
            return String.Format("{0:D4}{1:D2}{2:D2}", dt.Year, dt.Month, dt.Day);
        }

        private string GetRequiredTag(DICOMData data, uint tag)
        {
            if (data.Elements.ContainsKey(tag))
                return (string)data[tag].Data;
            DataDictionaryElement dde = DataDictionary.LookupElement(tag);
            throw new ArgumentNullException(dde.Group.ToString("X4") + "," + dde.Elem.ToString("X4") + ": " + dde.Description);
        }

        private string GetTagOrValue(DICOMData data, uint tag, string def, int maxLen)
        {
            //trim!
            string val = GetTagOrValue(data, tag, def);
            if (val.Length > maxLen) return val.Substring(0, maxLen);
            return val;
        }

        private string GetTagOrValue(DICOMData data, uint tag, string def)
        {
            if (data.Elements.ContainsKey(tag))
                return data[tag].Display.Replace('\0', ' ');
            return def;
        }

        private string GetDate(DICOMData data, uint tag, bool required = false)
        {
            if (data.Elements.ContainsKey(tag))
            {
                var date = data[tag].Display.Replace('\0', ' ').Replace(".", "").Replace("/", "").Replace("-", "");
                if (date.Length > 8)
                    return date.Substring(0, 8);
                return date;
            }
            if (required)
            {
                DataDictionaryElement dde = DataDictionary.LookupElement(tag);
                throw new ArgumentNullException(dde.Group.ToString("X4") + "," + dde.Elem.ToString("X4") + ": " + dde.Description);
            }
            return "";
        }

        private string GetTime(DICOMData data, uint tag, bool required = false)
        {
            if (data.Elements.ContainsKey(tag))
            {
                var time = data[tag].Display.Replace('\0', ' ').Replace(":", "");
                if (time.Length > 16)
                    return time.Substring(0, 16);
                return time;
            }
            if (required)
            {
                DataDictionaryElement dde = DataDictionary.LookupElement(tag);
                throw new ArgumentNullException(dde.Group.ToString("X4") + "," + dde.Elem.ToString("X4") + ": " + dde.Description);
            }
            return "";
        }

        public List<PSStudyBrowserSearchResult> StudySearch(PSStudyBrowserSearch search, int maxResults)
        {
            string query = "SELECT";

            if (this._databaseType == DBType.MSSQL)
            {
                query += " TOP " + maxResults.ToString();
            }

            query += " *, Studies.NumSeries StudiesNumSeries, Studies.NumImages StudiesNumImages, Patients.NumSeries PatientsNumSeries, Patients.NumImages PatientsNumImages FROM Studies INNER JOIN Patients ON Studies.IntPatID = Patients.IntPatID";

            var wheres = new List<string>();
            if (search.StartDate.HasValue)
            {
                wheres.Add("Studies.StuDate >= '" + GetDicomDate(search.StartDate.Value) + "'");
            }
            if (search.EndDate.HasValue)
            {
                wheres.Add("Studies.StuDate <= '" + GetDicomDate(search.EndDate.Value) + "'");
            }
            if (search.AccessionNum != null)
            {
                wheres.Add("Studies.AccessionNum LIKE '" + Escape(search.AccessionNum) + "%'");
            }
            if (search.Description != null)
            {
                if (search.Description.Length == 2)
                {
                    wheres.Add("(Studies.Modality = '" + Escape(search.Description) + "' OR Studies.StuDesc LIKE '" + Escape(search.Description) + "%')");
                }
                else
                {
                    wheres.Add("Studies.StuDesc LIKE '" + Escape(search.Description) + "%'");
                }
            }
            if (search.PatId != null)
            {
                wheres.Add("Studies.PatID LIKE '" + Escape(search.PatId) + "%'");
            }
            if (search.PatName != null)
            {
                wheres.Add("Patients.PatName LIKE '" + Escape(search.PatName) + "%'");
            }

            if (wheres.Count > 0)
            {
                query += " WHERE " + String.Join(" AND ", wheres);
            }

            query += " ORDER BY Studies.StuDate DESC";

            if (this._databaseType == DBType.MySQL || this._databaseType == DBType.SQLite)
            {
                query += " LIMIT " + maxResults.ToString();
            }

            var results = new List<PSStudyBrowserSearchResult>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        var pat = new PSPatient(reader);
                        var stu = new PSStudy(reader);
                        results.Add(new PSStudyBrowserSearchResult(pat, stu));
                    }
                    reader.Close();
                }
            }

            return results;
        }

        public IEnumerable<PSStudySnapshotExtended> GetPatientSnapshot(string patId, int maxResults)
        {
            string query = "SELECT";

            if (this._databaseType == DBType.MSSQL)
            {
                query += " TOP " + maxResults.ToString();
            }

            query += " *, Studies.NumSeries StudiesNumSeries, Series.NumImages SeriesNumImages FROM Studies INNER JOIN Series ON Series.IntStuID = Studies.IntStuID";

            var wheres = new List<string>();
            if (patId != null)
            {
                wheres.Add("Studies.PatID = '" + Escape(patId) + "'");
            }

            if (wheres.Count > 0)
            {
                query += " WHERE " + String.Join(" AND ", wheres);
            }

            if (this._databaseType == DBType.MySQL)
            {
                query += " LIMIT " + maxResults.ToString();
            }

            var results = new Dictionary<string, PSStudySnapshotExtended>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        var stu = new PSStudySnapshotExtended(reader);
                        var ser = new PSSeriesSnapshot(reader);
                        if (!results.ContainsKey(stu.StuInstID))
                        {
                            stu.Series.Add(ser);
                            results[stu.StuInstID] = stu;
                        }
                        else
                        {
                            results[stu.StuInstID].Series.Add(ser);
                        }
                    }
                    reader.Close();
                }
            }

            // Sort the results
            var resList = results.Values;
            var resListOrdered = resList.OrderByDescending(stu => stu.StuDateTime).ToList();

            foreach (var stu in resListOrdered)
            {
                stu.Series = stu.Series.OrderBy(ser => ser.SerNum).ToList();
            }

            return resListOrdered;
        }

        public List<PSImage> FetchStudyImages(string studyInstanceUID)
        {
            string query = "SELECT Images.* FROM Images INNER JOIN Series ON Images.IntSerID = Series.IntSerID INNER JOIN Studies ON Series.IntStuID = Studies.IntStuID " +
                "WHERE Studies.StuInstID = '" + Escape(studyInstanceUID) + "' " +
                "ORDER BY Series.SerNum, Images.ImaNum";

            var results = new List<PSImage>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        results.Add(new PSImage(reader));
                    }
                    reader.Close();
                }
            }

            return results;
        }

        public List<PSImage> FetchSeriesImages(string seriesInstanceUID)
        {
            string query = "SELECT Images.* FROM Images INNER JOIN Series ON Images.IntSerID = Series.IntSerID " +
                "WHERE Series.SerInstID = '" + Escape(seriesInstanceUID) + "' " +
                "ORDER BY Images.ImaNum";

            var results = new List<PSImage>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        results.Add(new PSImage(reader));
                    }
                    reader.Close();
                }
            }

            return results;
        }

        public List<PSImage> FetchImages(string[] imageInstanceUIDs)
        {
            string query = "SELECT Images.* FROM Images INNER JOIN Series ON Images.IntSerID = Series.IntSerID " +
                "WHERE Images.ImaInstID IN (" + string.Join(",", imageInstanceUIDs.Select(id => "'" + Escape(id) + "'")) + ") " +
                "ORDER BY Series.SerNum, Images.ImaNum";

            var results = new List<PSImage>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        results.Add(new PSImage(reader));
                    }
                    reader.Close();
                }
            }

            return results;
        }

        public List<PSTaskQueueItem> GetTaskQueue()
        {
            string query = "SELECT * FROM TaskQueue ORDER BY TaskId ASC";

            var results = new List<PSTaskQueueItem>();

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        results.Add(new PSTaskQueueItem(reader));
                    }
                    reader.Close();
                }
            }

            return results;
        }

        public void AddTaskQueueItem(PSTaskQueueItem item)
        {
            lock (_connection)
            {
                ExecuteNonQuery("INSERT INTO TaskQueue (Description, TaskType, TaskDataSerialized) VALUES ('" + Escape(item.Description) + "', " +
                    (int) item.TaskType + ", '" + Escape(item.TaskDataSerialized) + "')");
                item.TaskId = (uint) this.GetIdentityInsertID();
            }
        }

        public void CompleteTaskQueueItem(uint taskId)
        {
            lock (_connection)
            {
                ExecuteNonQuery("DELETE FROM TaskQueue WHERE TaskId = " + taskId.ToString());
            }
        }

        public QRResponseData GetQRResponse(QRRequestData request, bool files)
        {
            QRResponseData response = request.GenerateResponse();

            string query = "SELECT ";

            //find select columns
            List<string> rets = new List<string>();
            if (files)
            {
                rets.Add("Images.Path AS ImagesPath");
                rets.Add("Images.SOPClassID AS ImagesSOPClassID");
                rets.Add("Images.TransferSyntaxID AS ImagesTransferSyntaxID");                
            }

            List<uint> tagsSearching = new List<uint>();

            foreach (uint tag in response.TagsToFill)
            {
                if (fieldSources.ContainsKey(tag))
                {
                    stFieldSource field = fieldSources[tag];
                    if (request.FindLevel == QRLevelType.Series && field.fieldSource.Split('.')[0] == "Images")
                        continue;
                    if (request.FindLevel == QRLevelType.Study &&
                        (field.fieldSource.Split('.')[0] == "Series" || field.fieldSource.Split('.')[0] == "Images"))
                        continue;
                    if (request.FindLevel == QRLevelType.Patient &&
                        (field.fieldSource.Split('.')[0] == "Studies" || field.fieldSource.Split('.')[0] == "Series" || field.fieldSource.Split('.')[0] == "Images"))
                        continue;

                    tagsSearching.Add(tag);
                    rets.Add(fieldSources[tag].fieldSource + " AS " + fieldSources[tag].fieldSource.Replace(".", ""));
                }
            }

            foreach (uint tag in response.SPSSTags)
            {
                if (fieldSources.ContainsKey(tag))
                {
                    stFieldSource field = fieldSources[tag];
                    tagsSearching.Add(tag);
                    rets.Add(fieldSources[tag].fieldSource + " AS " + fieldSources[tag].fieldSource.Replace(".", ""));
                }
            }

            query += String.Join(",", rets.ToArray());

            //find tables to join to
            if (files || request.FindLevel == QRLevelType.Image)
            {
                query += " FROM Images INNER JOIN Series ON Images.IntSerID = Series.IntSerID INNER JOIN Studies ON Series.IntStuID = Studies.IntStuID INNER JOIN Patients ON Studies.IntPatID = Patients.IntPatID";
            }
            else if (request.FindLevel == QRLevelType.Patient)
            {
                query += " FROM Patients";
            }
            else if (request.FindLevel == QRLevelType.Study)
            {
                query += " FROM Studies INNER JOIN Patients ON Studies.IntPatID = Patients.IntPatID";
            }
            else if (request.FindLevel == QRLevelType.Series)
            {
                query += " FROM Series INNER JOIN Studies ON Series.IntStuID = Studies.IntStuID INNER JOIN Patients ON Studies.IntPatID = Patients.IntPatID";
            }

            List<string> qualifiers = new List<string>();
            foreach (uint tag in request.SearchTerms.Keys)
            {
                if (fieldSources.ContainsKey(tag))
                {
                    stFieldSource field = fieldSources[tag];
                    string searchTerm = request.SearchTerms[tag].ToString();
                    if (tag == DICOMTags.PatientName)
                    {
                        // People use commas to search for names
                        searchTerm = searchTerm.Replace(',', '^');

                        // Make sure that the name field ends with an automatic wildcard, it seems to be expected, despite being against the DICOM spec.
                        if (!searchTerm.EndsWith("*"))
                        {
                            searchTerm += "*";
                        }
                    }

                    if (field.ranged && searchTerm.Contains('-'))
                    {
                        //ranged value.  split and between.
                        string[] vals = searchTerm.Split('-');
                        if (vals[0] == "" && vals[1] != "")
                            qualifiers.Add(field.fieldSource + " <= '" + Escape(vals[1]) + "'");
                        else if (vals[0] != "" && vals[1] == "")
                            qualifiers.Add(field.fieldSource + " >= '" + Escape(vals[0]) + "'");
                        else if (vals[0] != "" && vals[1] != "")
                            qualifiers.Add(field.fieldSource + " BETWEEN '" + Escape(vals[0]) + "' AND '" + Escape(vals[1]) + "'");
                    }
                    else if (searchTerm.Contains('*'))
                    {
                        //has a wildcard
                        qualifiers.Add(field.fieldSource + " LIKE '" + Escape(searchTerm.Replace('*', '%')) + "'");
                    }
                    else
                    {
                        qualifiers.Add(field.fieldSource + " = '" + Escape(searchTerm) + "'");
                    }
                }
            }

            if (qualifiers.Count > 0)
                query += " WHERE " + String.Join(" AND ", qualifiers.ToArray());

            lock (_connection)
            {
                using (DbDataReader reader = ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        if (files)
                        {
                            string filePath = FixImagePath((string)reader["ImagesPath"]);
                            string abstractSyntax = (string)reader["ImagesSOPClassID"];
                            string transferSyntax = (string)reader["ImagesTransferSyntaxID"];

                            response.AddResponseFile(filePath, AbstractSyntaxes.Lookup(abstractSyntax), TransferSyntaxes.Lookup(transferSyntax));
                        }
                        else
                        {
                            Dictionary<uint, object> respRow = new Dictionary<uint, object>();
                            foreach (uint tag in tagsSearching)
                            {
                                stFieldSource field = fieldSources[tag];

                                respRow[tag] = reader[field.fieldSource.Replace(".", "")].ToString();
                            }

                            response.AddResponseRow(respRow);
                        }
                    }
                    reader.Close();
                }
            }

            return response;
        }

        public string FixImagePath(string inPath)
        {
            if (inPath.StartsWith(@"~/") && !string.IsNullOrEmpty(this._tildePathReplace))
            {
                return this._tildePathReplace + inPath.Substring(2);
            }

            return inPath;
        }


        public static DateTime GetDateTimeFromDbDatetime(object col)
        {
            if (col != null)
            {
                if (col is long || col is int || col is double || col is float)
                {
                    // UNIX Timestamp
                    return DateUtil.UnixTimeStampToDateTime(Convert.ToDouble(col));
                }
                if (col is DateTime)
                {
                    return (DateTime)col;
                }
                Debug.Assert(false);
            }
            return DateTime.MinValue;
        }

        public static DateTime GetDateTimeFromDbDateAndTimeStrings(string date, string time = null)
        {
            if (time != null)
            {
                string timeFrac = null;
                string timeWhole = time;
                if (time.Contains('.'))
                {
                    timeFrac = time.Substring(time.IndexOf('.') + 1);
                    timeWhole = time.Substring(0, time.IndexOf('.'));
                }

                while (timeWhole.Length < 6)
                {
                    timeWhole = "0" + timeWhole;
                }
                time = timeWhole;
                if (timeFrac != null)
                {
                    time += "." + timeFrac;
                }
            }
            return DateUtil.ConvertDicomDateAndTime(date, time);
        }


        public bool IsConnected { get { return _connection != null && _connection.State == System.Data.ConnectionState.Open; } }

        public bool IsSetup { get; private set; }

        public long TotalSizeKB { get; private set; }

        public uint PruneDBSizeMB { get; set; }

        private DbConnection _connection;

        public enum DBType
        {
            /// <summary>
            /// Indicates a MySQL database server.
            /// </summary>
            MySQL = 0,
            /// <summary>
            /// Indicates any MSSQL database server.  The SqlConnection documentation currently says that it 
            /// only works with SQL Server 2005 and above, but there's lots of anecdotal evidence of it
            /// working on SQL Server 2000 as well, so YMMV.
            /// </summary>
            MSSQL = 3,
            /// <summary>
            /// Indicates a SQLite database.
            /// </summary>
            SQLite = 4
        }

        private DBType _databaseType;

        private string _databaseName;

        private ILogger _logger;
    }
}
