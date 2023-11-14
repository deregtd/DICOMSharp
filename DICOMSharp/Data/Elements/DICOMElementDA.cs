using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementDA : DICOMElementString
    {
        public DICOMElementDA(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "DA"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("DA"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
