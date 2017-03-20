using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementST : DICOMElementString
    {
        public DICOMElementST(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "ST"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("ST"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
