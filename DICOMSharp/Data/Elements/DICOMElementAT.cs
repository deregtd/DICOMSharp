using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementAT : DICOMElement
    {
        public DICOMElementAT(ushort group, ushort elem)
            : base(group, elem)
        {
            //PS 3.5, Page 25
            //Ordered Pair of Attribute Tags (group then elem)
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, bool explicitVR)
        {
            //Sometimes there's 0-length ATs...
            uint readCount = length;
            if (readCount >= 2)
            {
                encapgroup = br.ReadUInt16();
                readCount -= 2;
            }
            else
            {
                encapgroup = 0;
            }

            if (readCount >= 2)
            {
                encapelem = br.ReadUInt16();
                readCount -= 2;
            }
            else
            {
                encapelem = 0;
            }

            br.ReadBytes((int) readCount);
            
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, bool explicitVR)
        {
            bw.Write(encapgroup);
            bw.Write(encapelem);
        }

        public override string Display
        {
            get { return "(" + string.Format("{0:X4}", encapgroup) + "," + string.Format("{0:X4}", encapelem) + ")"; }
        }

        public override string VR
        {
            get { return "AT"; }
        }

        public override object Data
        {
            get
            {
                return ((uint)encapgroup << 16) | encapelem;
            }
            set
            {
                if (value is uint)
                {
                    encapgroup = (ushort)((uint)value >> 16);
                    encapelem = (ushort)value;
                }
                else
                    throw new NotSupportedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return 4;
        }

        private ushort encapgroup, encapelem;

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("AT"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
