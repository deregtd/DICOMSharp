using PSCommon.Utilities;
using System;
using System.Data.Common;

namespace PSCommon.Models
{
    public class PSImage
    {
        public string ImaInstID;
        public uint IntSerID;
        public string SOPClassID;
        public string TransferSyntaxID;
        public uint ImaNum;
        public uint FileSizeKB;
        public string Path;
        public string SendingAE;
        public DateTime LastUsedTime;

        public PSImage()
        {
        }

        public PSImage(DbDataReader reader)
        {
            ImaInstID = (string)reader["ImaInstID"];
            IntSerID = Convert.ToUInt32(reader["IntSerID"]);
            SOPClassID = (string)reader["SOPClassID"];
            TransferSyntaxID = (string)reader["TransferSyntaxID"];
            ImaNum = Convert.ToUInt32(reader["ImaNum"]);
            FileSizeKB = Convert.ToUInt32(reader["FileSizeKB"]);
            Path = (string)reader["Path"];
            SendingAE = (string)reader["SendingAE"];
            LastUsedTime = PSDatabase.GetDateTimeFromDbDatetime(reader["LastUsedTime"]);
        }
    }
}
