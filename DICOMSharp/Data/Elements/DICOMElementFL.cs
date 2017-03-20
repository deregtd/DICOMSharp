using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementFL : DICOMElement
    {
        public DICOMElementFL(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, bool explicitVR)
        {
            if (length < 4)
            {
                data = 0;
                br.ReadBytes((int)length);
            }
            else
            {
                data = br.ReadSingle();
                if (length > 4)
                    br.ReadBytes((int)length - 4);
            }
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, bool explicitVR)
        {
            bw.Write(data);
        }

        public override string Display
        {
            get { return data.ToString(); }
        }

        public override string VR
        {
            get { return "FL"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is float)
                    data = (float)value;
                else
                    throw new NotSupportedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return 4;
        }

        private float data;

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("FL"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
