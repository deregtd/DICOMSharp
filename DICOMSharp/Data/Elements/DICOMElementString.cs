using System;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    internal abstract class DICOMElementString : DICOMElement
    {
        public DICOMElementString(ushort group, ushort elem)
            : base(group, elem)
        {
            padChar = (byte) ' ';
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, bool explicitVR)
        {
            //reuse setter
            Data = System.Text.Encoding.ASCII.GetString(br.ReadBytes((int)length));
            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, bool explicitVR)
        {
            bw.Write(Encoding.ASCII.GetBytes(data));

            if ((length & 1) > 0)  //pad it to even
                bw.Write(padChar);
        }

        public override string Display
        {
            get
            {
                if (data == null)
                    data = "";

                if (data.Length > 64)
                    return data.Substring(0, 64) + "[more]";
                else
                    return data;
            }
        }

        public override object Data
        {
            get
            {
                if (data == null)
                    data = "";
                return data;
            }
            set
            {
                if (value is string)
                {
                    data = (string)value;
                    length = (uint) data.Length;
                }
                else if (value == null)
                {
                    data = "";
                    length = 0;
                }
                else
                    throw new NotImplementedException();
            }
        }

        public override uint GetDataLength(bool explicitVR)
        {
            uint lenPad = length;
            if ((lenPad & 1) == 1)      //must pad out to even...
                lenPad++;
            return lenPad;
        }

        protected byte padChar; 
        protected uint length;
        protected string data;
    }
}
