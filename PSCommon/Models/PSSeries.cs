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
    public class PSSeriesSnapshot
    {
        public string SerInstID;
        public int NumImages;
        public DateTime SerDateTime;
        public int SerNum;
        public string Modality;
        public string SerDesc;
        public string BodyPart;

        public PSSeriesSnapshot(DbDataReader reader)
        {
            SerInstID = (string)reader["SerInstID"];
            NumImages = Convert.ToInt32(reader["SeriesNumImages"]);
            SerDateTime = PSDatabase.GetDateTimeFromDbDateAndTimeStrings(reader["SerDate"].ToString(), reader["SerTime"].ToString());
            SerNum = Convert.ToInt32(reader["SerNum"]);
            Modality = (string)reader["Modality"];
            SerDesc = (string)reader["SerDesc"];
            BodyPart = (string)reader["BodyPart"];
        }
    }

    public class PSSeries : PSSeriesSnapshot
    {
        public uint IntSerID;
        public uint IntStuID;
        public uint SerSizeKB;
        public DateTime LastUsedTime;

        public PSSeries(DbDataReader reader) : base(reader)
        {
            IntSerID = (uint)reader["IntSerID"];
            IntStuID = (uint)reader["IntStuID"];
            SerSizeKB = (uint)reader["SerSizeKB"];
            LastUsedTime = PSDatabase.GetDateTimeFromDbDatetime(reader["LastUsedTime"]);
        }
    }
}
