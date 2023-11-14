using DICOMSharp.Network.Connections;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models
{
    public class PSEntity : ApplicationEntity
    {
        [Flags]
        public enum FlagsMask : int
        {
            None = 0,
            SendDestination = 1
        }

        public string Comment;
        public FlagsMask Flags;

        // Private empty constructor for JSON deserialization 
        private PSEntity()
        {
        }

        public PSEntity(string title, string address, ushort port, FlagsMask flags, string comment)
            : base(title, address, port)
        {
            this.Comment = comment;
            this.Flags = flags;
        }

        public PSEntity(DbDataReader reader)
            : base((string)reader["AE"], (string)reader["Address"], Convert.ToUInt16(reader["Port"]))
        {
            this.Comment = (string)reader["Comment"];
            this.Flags = (FlagsMask) Convert.ToInt32(reader["Flags"]);
        }
    }
}
