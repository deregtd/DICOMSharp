﻿using System;
using System.Text;

namespace DICOMSharp.Data.Elements
{
    internal class DICOMElementUT : DICOMElementString
    {
        public DICOMElementUT(ushort group, ushort elem)
            : base(group, elem)
        {
        }

        public override string VR
        {
            get { return "UT"; }
        }

        public readonly static ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("UT"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
