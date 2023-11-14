using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementFD : DICOMElement
    {
        public DICOMElementFD(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            if (length < 8)
            {
                data = 0;
                br.ReadBytes((int)length);
            }
            else
            {
                data = br.ReadDouble();
                if (length > 8)
                    br.ReadBytes((int)length - 8);
            }
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            bw.Write(data);
        }

        public override string Display
        {
            get { return data.ToString(); }
        }

        public override string VR
        {
            get { return "FD"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is double)
                    data = (double)value;
                else
                    throw new NotSupportedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return 8;
        }

        private double data;

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("FD"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
