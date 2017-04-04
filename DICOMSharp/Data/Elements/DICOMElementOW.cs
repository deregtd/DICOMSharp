using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementOW : DICOMElement
    {
        public DICOMElementOW(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            //Store it as LSB-ordered bytes for optimization
            //Maybe down the line add some optimized way to modify a word array if it looks like a user's gonna be doing that a lot for some reason
            Data = br.ReadWords((int)length);
            return this.length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            bw.WriteWords(data);
        }

        public override string Display
        {
            get { return "[OW Binary Data]"; }
        }

        public override string VR
        {
            get { return "OW"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is byte[])
                {
                    data = (byte[])value;
                    length = (uint)data.Length;
                }
                else if (value is ushort[])
                {
                    //split the ushorts into bytes...
                    ushort[] ndata = (ushort[])value;
                    data = new byte[ndata.Length * 2];
                    length = (uint)data.Length;
                    for (int i = 0; i < ndata.Length; i++)
                    {
                        data[i * 2] = (byte)ndata[i];
                        data[i * 2 + 1] = (byte)(ndata[i] >> 8);
                    }
                }
                else
                    throw new NotSupportedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return length;
        }

        private uint length;
        private byte[] data;

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("OW"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
