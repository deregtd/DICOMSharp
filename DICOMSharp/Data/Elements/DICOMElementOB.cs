using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementOB : DICOMElement
    {
        internal DICOMElementOB(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            //reuse the setter!
            byte[] data = br.ReadBytesUpToMax((int)length);
            Data = data;
            return (uint) data.Length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            bw.Write(data);
        }

        public override string Display
        {
            get { return "[OB Binary Data]"; }
        }

        public override string VR
        {
            get { return "OB"; }
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
                    data = (byte[]) value;
                    length = (uint) data.Length;
                }
                else if (value is sbyte || value is byte)
                {
                    data = BitConverter.GetBytes((byte)value);
                    length = 1;
                }
                else if (value is short || value is ushort)
                {
                    data = BitConverter.GetBytes((ushort)value);
                    length = 2;
                }
                else if (value is int || value is uint)
                {
                    data = BitConverter.GetBytes((uint)value);
                    length = 4;
                }
                else if (value is long || value is ulong)
                {
                    data = BitConverter.GetBytes((ulong)value);
                    length = 8;
                }
                else if (value is float)
                {
                    data = BitConverter.GetBytes((float)value);
                    length = 4;
                }
                else if (value is double)
                {
                    data = BitConverter.GetBytes((double)value);
                    length = 8;
                }
                else if (value is decimal)
                {
                    data = BitConverter.GetBytes((double) (decimal) value);
                    length = 8;
                }
                else if (value is string)
                {
                    data = ASCIIEncoding.ASCII.GetBytes((string)value);
                    length = (uint) data.Length;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            return length;
        }

        private uint length;
        private byte[] data;

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("OB"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
