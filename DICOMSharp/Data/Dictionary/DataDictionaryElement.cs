using System.Diagnostics;
using System.Text;

namespace DICOMSharp.Data.Dictionary
{
    /// <summary>
    /// This class contains information about a Data Dictionary Element.  This can be useful
    /// for debugging displays for DICOM dumps.
    /// </summary>
    public class DataDictionaryElement
    {
        internal DataDictionaryElement(ushort group, ushort elem, string desc, string vr, string vm, bool retired)
        {
            Group = group;
            Elem = elem;
            Description = desc;
            VR = vr;

            if (vr.Length < 2) vr = vr.PadLeft(2, '?');
            byte[] vrbytes = Encoding.ASCII.GetBytes(vr);
            vrshort = (ushort)(vrbytes[0] | (vrbytes[1] << 8));

            Tag = ((uint)Group << 16) | (uint)Elem;

            string[] vms = vm.Split('-');
            if (vms.Length == 1)
            {
                if (vms[0] == "")
                    VMMin = VMMax = 1;
                else
                    VMMin = VMMax = int.Parse(vms[0]);
            }
            else if (vms.Length == 2)
            {
                VMMin = int.Parse(vms[0]);
                if (vms[1] == "n" || vms[1] == "2n" || vms[1] == "3n")
                    VMMax = int.MaxValue;
                else
                    VMMax = int.Parse(vms[1]);
            }
            else
            {
                Debug.Assert(false);
                VMMin = 1;
                VMMax = 1;
            }
        }

        /// <summary>
        /// The DICOM Group number for the element.
        /// </summary>
        public ushort Group { get; private set; }

        /// <summary>
        /// The DICOM Element number for the element.
        /// </summary>
        public ushort Elem { get; private set; }

        /// <summary>
        /// The DICOM Value Representation (VR) for the element.
        /// </summary>
        public string VR { get; private set; }

        /// <summary>
        /// The description from the DICOM Spec (Part 6) for the element.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Whether the latest DICOM Spec (Part 6) has this field marked as retired or not.
        /// </summary>
        public bool Retired { get; private set; }

        /// <summary>
        /// The Value Multiplicity Minimum
        /// </summary>
        public int VMMin { get; private set; }

        /// <summary>
        /// The Value Multiplicity Maximum (int.max for "n")
        /// </summary>
        public int VMMax { get; private set; }



        /// <summary>
        /// The combined group/elem as a DICOMTag.
        /// </summary>
        public uint Tag { get; private set; }


        internal ushort vrshort;
    }
}
