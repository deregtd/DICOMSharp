using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Data.Dictionary;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementUS : DICOMElement
    {
        public DICOMElementUS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            if (length < 2)
            {
                //busted...
                data = (ushort) 0;
                br.ReadBytes((int)length);
            }
            else if (length == 2)
            {
                data = br.ReadUInt16();
            }
            else if (length > 2)
            {
                uint readBytes = 0;
                DataDictionaryElement elemCheck = DataDictionary.LookupElement(this.Tag);
                if (elemCheck == null || elemCheck.VMMax > 1)
                {
                    //Unknown VM or VM goes to > 1
                    data = new ushort[length / 2];
                    for (int i = 0; i < length / 2; i++)
                        ((ushort[])data)[i] = br.ReadUInt16();
                    readBytes = (length / 2) * 2;
                }
                else
                {
                    //VM of 1 -- force to single data point
                    data = br.ReadUInt16();
                    readBytes = 2;
                }
                if (length - readBytes > 0)
                    br.ReadBytes((int) (length - readBytes));
            }
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            if (data is ushort)
                bw.Write((ushort)data);
            else
            {
                foreach (ushort de in (ushort[])data)
                    bw.Write(de);
            }
        }

        public override string Display
        {
            get {
                if (data is ushort)
                    return data.ToString();
                else
                {
                    string[] strs = new string[((ushort[])data).Length];
                    for (int i=0; i<strs.Length; i++)
                        strs[i] = ((ushort[])data)[i].ToString();
                    return String.Join("\\", strs);
                }
            }
        }

        public override string VR
        {
            get { return "US"; }
        }

        public override object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value is ushort || value is short || value is int)
                    data = (ushort)value;
                else if (value is ushort[])
                    data = value;
                else
                    throw new NotImplementedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            if (data is ushort)
                return 2;
            else
                return 2 * (uint) ((ushort[])data).Length;
        }

        private object data;

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("US"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
