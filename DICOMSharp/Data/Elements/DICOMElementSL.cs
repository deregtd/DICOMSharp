using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementSL : DICOMElement
    {
        public DICOMElementSL(ushort group, ushort elem)
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
                data = br.ReadInt32();
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
            get { return "SL"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is int || value is uint)
                    data = (int)value;
                else
                    throw new NotImplementedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return 4;
        }

        private int data;

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("SL"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
