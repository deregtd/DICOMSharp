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
    public class PSStudySnapshot
    {
        public string StuInstID;
        public DateTime StuDateTime;
        public string AccessionNum;
        public string Modality;
        public string StuDesc;

        public PSStudySnapshot(DbDataReader reader)
        {
            StuInstID = (string)reader["StuInstID"];
            StuDateTime = PSDatabase.GetDateTimeFromDbDateAndTimeStrings(reader["StuDate"].ToString(), reader["StuTime"].ToString());
            AccessionNum = (string)reader["AccessionNum"];
            Modality = (string)reader["Modality"];
            StuDesc = (string)reader["StuDesc"];
        }
    }

    public class PSStudySnapshotExtended : PSStudySnapshot
    {
        public List<PSSeriesSnapshot> Series;

        public PSStudySnapshotExtended(DbDataReader reader): base(reader)
        {
            Series = new List<PSSeriesSnapshot>();
        }
    }

    public class PSStudy : PSStudySnapshot
    {
        public int IntStuID;
        public int IntPatID;
        public string PatID;
        public int NumSeries;
        public int NumImages;
        public int StuSizeKB;
        public string StuID;
        public string RefPhysician;
        public string DeptName;
        public DateTime LastUsedTime;

        public PSStudy(DbDataReader reader): base(reader)
        {
            IntStuID = Convert.ToInt32(reader["IntStuID"]);
            IntPatID = Convert.ToInt32(reader["IntPatID"]);
            PatID = (string)reader["PatID"];
            NumSeries = Convert.ToInt32(reader["StudiesNumSeries"]);
            NumImages = Convert.ToInt32(reader["StudiesNumImages"]);
            StuSizeKB = Convert.ToInt32(reader["StuSizeKB"]);
            StuID = (string)reader["StuID"];
            RefPhysician = (string)reader["RefPhysician"];
            DeptName = (string)reader["DeptName"];
            LastUsedTime = PSDatabase.GetDateTimeFromDbDatetime(reader["LastUsedTime"]);
        }
    }
}
