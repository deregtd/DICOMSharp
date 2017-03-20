using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementDS : DICOMElementString
    {
        public DICOMElementDS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "DS"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("DS"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
