using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementUN : DICOMElementString
    {
        public DICOMElementUN(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "UN"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("UN"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
