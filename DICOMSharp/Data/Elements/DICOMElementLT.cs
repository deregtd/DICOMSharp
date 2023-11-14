using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementLT : DICOMElementString
    {
        public DICOMElementLT(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "LT"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("LT"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
