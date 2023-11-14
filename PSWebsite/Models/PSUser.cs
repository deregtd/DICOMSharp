using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models
{
    public class PSUser
    {
        [Flags]
        public enum UserAccess
        {
            None = 0,
            Reader = 1,
            ServerAdmin = 2,
            StudySend = 4,
            StudyDelete = 8
        }

        public class UserInfo
        {
            public string username;
            public string realname;
            public UserAccess access;
        }
        
        public string username;
        public string password;
        public string realname;
        public UserAccess access;

        public string lastIP;
        public DateTime lastAction;

        public PSUser()
        {
        }

        public PSUser(DbDataReader reader, bool populatePassword)
        {
            username = (string)reader["Username"];
            if (populatePassword)
            {
                password = (string)reader["Password"];
            }
            realname = (string)reader["Realname"];
            access = (UserAccess)Convert.ToInt32(reader["Access"]);
            lastIP = (reader["Remoteip"] is DBNull) ? null : (string)reader["Remoteip"];
            if (reader.GetOrdinal("Lastaction") < 0)
            {
                // Some weird bug in the SQLite .NET connector is causing this to show up when null the first time...
                lastAction = DateTime.MinValue;
            } else {
                lastAction = (reader["Lastaction"] is DBNull) ? DateTime.MinValue : (DateTime)reader["Lastaction"];
            }
        }

        public UserInfo GetUserInfo()
        {
            return new UserInfo()
            {
                username = username,
                realname = realname,
                access = access
            };
        }
    }
}
