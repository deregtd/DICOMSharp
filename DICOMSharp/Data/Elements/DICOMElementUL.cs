using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementUL : DICOMElement
    {
        public DICOMElementUL(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            if (length < 4)
            {
                data = 0;
                br.ReadBytes((int)length);
            }
            else
            {
                data = br.ReadUInt32();
                if (length > 4)
                    br.ReadBytes((int)length - 4);
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
            get { return "UL"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is uint || value is int)
                    data = (uint)value;
                else
                    throw new NotImplementedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return 4;
        }

        private uint data;

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("UL"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
