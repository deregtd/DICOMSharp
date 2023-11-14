using DICOMSharp.Util;
using PSCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models
{
    public class PSPatient
    {
        public int IntPatID;
        public string PatID;
        public string PatName;
        public DateTime PatBirthDate;
        public string PatSex;
        public int NumStudies;
        public int NumSeries;
        public int NumImages;
        public float PatSizeKB;
        public DateTime LastUsedTime;

        public PSPatient()
        {
        }

        public PSPatient(DbDataReader reader)
        {
            IntPatID = Convert.ToInt32(reader["IntPatID"]);
            PatID = (string)reader["PatID"];
            PatName = (string)reader["PatName"];
            PatBirthDate = PSDatabase.GetDateTimeFromDbDateAndTimeStrings(reader["PatBirthDate"].ToString());
            PatSex = (string)reader["PatSex"];
            NumStudies = Convert.ToInt32(reader["NumStudies"]);
            NumSeries = Convert.ToInt32(reader["PatientsNumSeries"]);
            NumImages = Convert.ToInt32(reader["PatientsNumImages"]);
            PatSizeKB = Convert.ToSingle(reader["PatSizeKB"]);
            LastUsedTime = PSDatabase.GetDateTimeFromDbDatetime(reader["LastUsedTime"]);
        }
    }
}
