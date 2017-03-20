using System.Collections.Generic;
using System.Linq;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using Newtonsoft.Json.Linq;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    /// <summary>
    /// This class represents an individual item in a DICOM SQ (sequence) element.  It can encapsulate either image data for an encapsulated
    /// image or a collection of other DICOM elements (which may, in turn, contain other sequence elements with their own SQItems).
    /// </summary>
    public class SQItem
    {
        /// <summary>
        /// Creates an empty SQItem with no encapsulated data or child elements.
        /// </summary>
        public SQItem(DICOMElementSQ parentElem)
        {
            _parentElem = parentElem;

            ElementsLookup = new SortedDictionary<uint, DICOMElement>();
            Elements = new List<DICOMElement>();
            lengthValid = false;
            EncapsulatedImageData = null;
        }

        internal uint ParseStream(SwappableBinaryReader br, ILogger logger, bool explicitVR, uint length, bool encapImage)
        {
            IsEncapsulatedImage = encapImage;

            if (encapImage && length != 0xFFFFFFFF)
            {
                //Encapsulated Image with explicit length!
                EncapsulatedImageData = br.ReadBytes((int)length);

                //Any way to figure out if we need to MSB-swap this? (i.e. is it an OW or an OB? Does that come from the parent element?)
                return length;
            }

            bool done = false;
            uint count = 0;
            while (!done)
            {
                //Check if we're past the end yet
                if (length != 0xFFFFFFFF && count >= length)
                {
                    done = true;
                    continue;
                }

                long readPosition = br.BaseStream.Position;

                //Read in group/element info
                ushort group = br.ReadUInt16();
                ushort elem = br.ReadUInt16();
                count += 4;

                //Make element
                uint outLen;
                DICOMElement nelem = DICOMElement.Parse(group, elem, logger, explicitVR, br, this, false, out outLen);

                //Store reading position in case it's useful later
                nelem.ReadPosition = readPosition;
                count += outLen;

                //Was that the end-of-sequence item?
                if (group == 0xFFFE && elem == 0xE00D)
                    done = true;
                else
                {
                    ElementsLookup[nelem.Tag] = nelem;
                    Elements.Add(nelem);
                }
            }
            return count;
        }

        internal void WriteData(SwappableBinaryWriter bw, ILogger logger, bool explicitVR)
        {
            if (IsEncapsulatedImage)
            {
                //Encapsulated image... just write out the raw image data. this may or may not be correct...
                bw.Write(EncapsulatedImageData);
            }
            else
            {
                //Normal sequence

                //header already written, just write the contents of your elements
                foreach (DICOMElement elem in Elements)
                {
                    //Store write position for later possible use
                    elem.WritePosition = bw.BaseStream.Position;

                    //Have the element write itself out.
                    elem.Write(bw, logger, explicitVR);
                }
            }
        }

        internal uint GetLength(bool explicitVR)
        {
            if (!lengthValid || explicitVR != lengthCalcExpVR)
                RecalcLength(explicitVR);

            return length;
        }

        private void RecalcLength(bool explicitVR)
        {
            //Calculate length
            length = 0;
            foreach (DICOMElement elem in Elements)
                length += elem.GetLength(explicitVR);
            if (IsEncapsulatedImage && EncapsulatedImageData != null)
                length += (uint)EncapsulatedImageData.Length;

            lengthCalcExpVR = explicitVR;
            lengthValid = true;
        }

        /// <summary>
        /// Dumps the contents of the SQItem to a string for debugging/logging purposes.
        /// </summary>
        /// <param name="nest">Number of chars (*2 this number) to indent the debug response.</param>
        /// <returns>The dumped contents as a string.</returns>
        public string Dump(int nest)
        {
            string nestStr = new string(' ', 2 * nest);
            string outstr = nestStr + "SQ Item:\n";
            foreach (DICOMElement elem in Elements)
            {
                outstr += nestStr + elem.Dump() + "\n";
                if (elem is DICOMElementSQ)
                    outstr += ((DICOMElementSQ)elem).GetDisplayContents(nest + 1);
            }
            if (IsEncapsulatedImage && EncapsulatedImageData != null)
                outstr += "[Encapsulated Image: " + EncapsulatedImageData.Length + " bytes]\n";
            return outstr;
        }

        /// <summary>
        /// Dumps the contents of the SQItem structure to a JSON object, usually for use with web clients.
        /// </summary>
        public JObject DumpJson()
        {
            var obj = new JObject();
            if (Elements.Count > 0)
            {
                obj["elements"] = new JArray(Elements.Select(elem => elem.DumpJson()));
            }
            if (IsEncapsulatedImage)
            {
                obj["data"] = "Encapsulated Image";
            }
            return obj;
        }

        /// <summary>
        /// Returns whether or not this SQItem has encapsulated image data or not.
        /// </summary>
        public bool IsEncapsulatedImage { get; private set; }

        /// <summary>
        /// Represents the encapsulated image data (if any exists).  If this value is assigned to, it turns the SQItem into an encapsulated image.
        /// </summary>
        public byte[] EncapsulatedImageData
        {
            get
            {
                return encapsulatedImageData;
            }
            set
            {
                encapsulatedImageData = value;
                IsEncapsulatedImage = (value != null);
                lengthValid = false;
            }
        }
        private byte[] encapsulatedImageData;

        private uint length;
        private bool lengthValid;
        private bool lengthCalcExpVR;

        /// <summary>
        /// Returns the parent DICOM Element (SQ) of the SQItem.
        /// </summary>
        public DICOMElementSQ ParentElement { get { return _parentElem; } }

        private DICOMElementSQ _parentElem;

        /// <summary>
        /// A sorted/indexed list of <see cref="DICOMElement"/>s contained in the SQItem.
        /// </summary>
        public SortedDictionary<uint, DICOMElement> ElementsLookup { get; private set; }

        /// <summary>
        /// An ordered list of <see cref="DICOMElement"/>s contained in the SQItem as they were originally stored.
        /// </summary>
        public List<DICOMElement> Elements { get; private set; }
    }
}
