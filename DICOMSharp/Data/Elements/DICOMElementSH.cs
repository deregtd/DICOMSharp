﻿using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementSH : DICOMElementString
    {
        public DICOMElementSH(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "SH"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("SH"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
