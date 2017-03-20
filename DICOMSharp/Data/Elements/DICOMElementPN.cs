using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementPN : DICOMElementString
    {
        public DICOMElementPN(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "PN"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("PN"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
