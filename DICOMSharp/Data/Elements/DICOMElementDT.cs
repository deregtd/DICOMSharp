using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementDT : DICOMElementString
    {
        public DICOMElementDT(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "DT"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("DT"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
