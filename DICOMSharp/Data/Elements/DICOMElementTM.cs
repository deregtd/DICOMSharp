using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementTM : DICOMElementString
    {
        public DICOMElementTM(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "TM"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("TM"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
