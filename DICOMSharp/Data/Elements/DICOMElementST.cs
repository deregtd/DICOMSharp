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

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("ST"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
