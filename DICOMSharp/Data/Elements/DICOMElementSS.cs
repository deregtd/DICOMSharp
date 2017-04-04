using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Data.Dictionary;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementSS : DICOMElement
    {
        public DICOMElementSS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            if (length < 2)
            {
                //busted...
                data = 0;
                br.ReadBytes((int) length);
            }
            else if (length == 2)
            {
                data = br.ReadInt16();
            }
            else if (length > 2)
            {
                uint readBytes = 0;
                DataDictionaryElement elemCheck = DataDictionary.LookupElement(this.Tag);
                if (elemCheck == null || elemCheck.VMMax > 1)
                {
                    //Unknown VM or VM goes to > 1
                    data = new short[length / 2];
                    for (int i = 0; i < length / 2; i++)
                        ((short[])data)[i] = br.ReadInt16();
                    readBytes = (length / 2) * 2;
                }
                else
                {
                    //VM of 1 -- force to single data point
                    data = br.ReadInt16();
                    readBytes = 2;
                }
                if (length - readBytes > 0)
                    br.ReadBytes((int)(length - readBytes));
            }
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            if (data is short)
                bw.Write((short)data);
            else
            {
                foreach (short de in (short[])data)
                    bw.Write(de);
            }
        }

        public override string Display
        {
            get
            {
                if (data is short)
                    return data.ToString();
                else
                {
                    string[] strs = new string[((short[])data).Length];
                    for (int i = 0; i < strs.Length; i++)
                        strs[i] = ((short[])data)[i].ToString();
                    return String.Join("\\", strs);
                }
            }
        }

        public override string VR
        {
            get { return "SS"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is short || value is ushort || value is int)
                    data = (short)value;
                else if (value is short[])
                    data = value;
                else
                    throw new NotSupportedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            if (data is short)
                return 2;
            else
                return 2 * (uint)((short[])data).Length;
        }

        private object data;

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("SS"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
