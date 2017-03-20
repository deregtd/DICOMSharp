using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementLO : DICOMElementString
    {
        public DICOMElementLO(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "LO"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("LO"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
