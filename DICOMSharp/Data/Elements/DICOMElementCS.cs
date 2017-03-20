using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementCS : DICOMElementString
    {
        public DICOMElementCS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "CS"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("CS"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
