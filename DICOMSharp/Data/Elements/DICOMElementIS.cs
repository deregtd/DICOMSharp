using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementIS : DICOMElementString
    {
        public DICOMElementIS(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "IS"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("IS"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
