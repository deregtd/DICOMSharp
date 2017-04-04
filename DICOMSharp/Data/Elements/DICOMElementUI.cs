using DICOMSharp.Data.Transfers;
using DICOMSharp.Logging;
using DICOMSharp.Util;
using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementUI : DICOMElementString
    {
        public DICOMElementUI(ushort group, ushort elem)
            : base(group, elem)
        {
            padChar = 0;
        }

        public override string VR
        {
            get { return "UI"; }
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            // Make sure to follow the UID rules
            bw.Write(Uid.UidToBytes(this.data));
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is string)
                {
                    data = Uid.SanitizeUid(value as string);
                }
                else if (value is Uid)
                {
                    data = (value as Uid).UidStr;
                }
                else if (value == null)
                {
                    data = "";
                }
                else
                    throw new NotImplementedException();

                length = (uint)data.Length;
            }
        }

        public override string Display
        {
            get
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    return "[Empty]";
                }

                if (Uid.MasterLookup.ContainsKey(data))
                {
                    return Uid.MasterLookup[data].ToString();
                }

                return data;
            }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("UI"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
