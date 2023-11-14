using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementAS : DICOMElementString
    {
        public DICOMElementAS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "AS"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("AS"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
