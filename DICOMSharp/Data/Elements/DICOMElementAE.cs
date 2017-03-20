using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementAE : DICOMElementString
    {
        public DICOMElementAE(ushort group, ushort elem)
            : base(group, elem)
        {
            //PS 3.5, Page 25
            //Spaces are ignored. Max of 16 chars, can be less.
        }

        public override string VR
        {
            get { return "AE"; }
        }

        public static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("AE"), 0);
        internal override ushort VRShort { get { return vrshort; } }
    }
}
