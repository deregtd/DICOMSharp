using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data.Tags;
using DICOMSharp.Util;
using Newtonsoft.Json.Linq;
using DICOMSharp.Logging;

namespace DICOMSharp.Data.Elements
{
    /// <summary>
    /// DICOMElementSQ represents a special type of DICOM Element, the SQ/Sequence element.
    /// It contains a list of sequence items, each of which has a sub-collection of DICOM Elements inside of it.
    /// Sometimes pixel data is contained withing sequence items as well.
    /// SQ elements can also be infinitely nested.
    /// </summary>
    public class DICOMElementSQ : DICOMElement
    {
        internal DICOMElementSQ(ushort group, ushort elem)
            : base(group, elem)
        {
            Items = new List<SQItem>();
        }

        internal override uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax)
        {
            //Covered by PS 3.5, Chapter 7.5 (pg. 42-45)
            bool encapImage = (Tag == DICOMTags.PixelData);

            bool done = false;
            uint count = 0;
            while (!done)
            {
                //If explicit length, see if we're done
                if (length != 0xFFFFFFFF && count >= length)
                {
                    done = true;
                    continue;
                }

                ushort grp = br.ReadUInt16();
                ushort elem = br.ReadUInt16();
                uint len = br.ReadUInt32();
                count += 8;

                //Sometimes there's retarded files that are msb swapped inside of sequences outside of the transfer syntax
                bool needMoreSwap = (grp == 0xFEFF);

                if (needMoreSwap)
                {
                    //Swap the reader for now, then swap back after the sqitem
                    br.ToggleSwapped();

                    //still have to swap the stuff we've already read by hand tho
                    grp = MSBSwapper.SwapW(grp);
                    elem = MSBSwapper.SwapW(elem);
                    len = MSBSwapper.SwapDW(len);
                }

                if (grp == 0xFFFE && elem == 0xE000)
                {
                    //Normal element, parse it out.
                    SQItem item = new SQItem(this);
                    count += item.ParseStream(br, logger, transferSyntax, len, encapImage);
                    Items.Add(item);
                }
                else if (grp == 0xFFFE && elem == 0xE0DD)
                {
                    //End element

                    //In case there's garbage in there, read the length bytes
                    br.ReadBytes((int)len);
                    count += len;

                    done = true;
                }
                else
                {
                    throw new NotImplementedException();
                }

                //toggle back in case it's just one SQItem that's hosed
                if (needMoreSwap)
                    br.ToggleSwapped();
            }

            return count;
        }

        /// <see cref="DICOMElement.GetDataLength"/>
        public override uint GetDataLength(bool explicitVR)
        {
            uint length = 0;
            foreach (SQItem item in Items)
            {
                length += 8;
                length += item.GetLength(explicitVR);
            }
            if (group == 0x7FE0 && elem == 0x0010)
                length += 8;

            return length;
        }

        internal override void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            //Covered by PS 3.5, Chapter 7.5 (pg. 42-45)

            foreach (SQItem item in Items)
            {
                //write FFFE,E000 item header
                bw.Write((ushort)0xFFFE);
                bw.Write((ushort)0xE000);
                bw.Write(item.GetLength(transferSyntax.ExplicitVR));

                //write your contents!
                item.WriteData(bw, logger, transferSyntax);
            }

            if (group == 0x7FE0 && elem == 0x0010)
            {
                //Image data, so it's encapsulated...  Write the SQ ender old school style.
                bw.Write((ushort)0xFFFE);
                bw.Write((ushort)0xE0DD);
                bw.Write((uint)0);
            }
        }

        /// <see cref="DICOMElement.Display"/>
        public override string Display
        {
            get { return "[Sequence...]"; }
        }

        /// <see cref="DICOMElement.DumpJson"/>
        public override JObject DumpJson()
        {
            var obj = base.DumpJson();
            obj["items"] = new JArray(Items.Select(item => item.DumpJson()));
            return obj;
        }

        internal string GetDisplayContents(int nest)
        {
            string outstr = "", nestStr = new string(' ', 2 * nest);
            foreach (SQItem item in Items)
                outstr += item.Dump(nest);
            return outstr;
        }

        /// <see cref="DICOMElement.VR"/>
        public override string VR
        {
            get { return "SQ"; }
        }

        /// <see cref="DICOMElement.Data"/>
        public override object Data
        {
            get
            {
                return Items;
            }
            set
            {
                if (value is List<SQItem>)
                    Items = (List<SQItem>) value;
            }
        }

        /// <summary>
        /// A list of the sequence items contained within this DICOMElement.  An SQItem contains its own set of DICOMElements, and a SQ-typed DICOMElement will have 1 or more of these SQItems.
        /// </summary>
        public List<SQItem> Items { get; private set; }

        public static readonly ushort vrshort = BitConverter.ToUInt16(Encoding.ASCII.GetBytes("SQ"), 0);
        public override ushort VRShort { get { return vrshort; } }
    }
}
