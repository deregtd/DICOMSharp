using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models
{
    public class PSLogEntry
    {
        public DateTime date;
        public string entry;

        public PSLogEntry(DbDataReader reader)
        {
            date = (DateTime)reader["Date"];
            entry = (string)reader["Entry"];
        }
    }
}
