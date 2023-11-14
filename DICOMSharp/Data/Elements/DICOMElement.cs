using System.IO;

using DICOMSharp.Data.Dictionary;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Data.Tags;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using DICOMSharp.Logging;
using System;

namespace DICOMSharp.Data.Elements
{
    /// <summary>
    /// The DICOMElement abstract class represents information for a generic DICOM Element.  You can use
    /// reflection to figure out what element type it is, or use its simple abstract methods and properties
    /// to use its generic attributes.
    /// </summary>
    public abstract class DICOMElement
    {
        internal DICOMElement(ushort group, ushort elem)
        {
            this.group = group;
            this.elem = elem;

            //calc
            this.groupelem = ((uint)group << 16) | elem;
        }

        abstract internal uint ParseData(SwappableBinaryReader br, ILogger logger, uint length, TransferSyntax transferSyntax);
        abstract internal void WriteData(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax);

        /// <summary>
        /// Dump the contents of the DICOM Element to a string, usually for debugging.
        /// </summary>
        /// <returns>The dumped contents as a string.</returns>
        public string Dump()
        {
            string ret = "(" + string.Format("{0:X4}", group) + "," + string.Format("{0:X4}", elem) + ") [" + GetDataLength(false).ToString() + "," + VR + "," + Description + "] " + Display;
            return ret;
        }

        /// <summary>
        /// Dumps the contents of the DICOMElement structure to a JSON object, usually for use with web clients.
        /// </summary>
        public virtual JObject DumpJson()
        {
            var obj = new JObject();
            obj["group"] = group;
            obj["elem"] = elem;
            obj["vr"] = VR;
            obj["desc"] = Description;

            if (VRShort == DICOMElementSQ.vrshort)
            {
                // don't bother with data elem
            }
            else
            {
                obj["len"] = GetDataLength(false);

                if (Data is byte[])
                {
                    if (groupelem == DICOMTags.PixelData)
                    {
                        // Don't bother
                    }
                    else
                    {
                        // Just doing it as a byte array seems to make it a string...
                        obj["data"] = new JArray((Data as byte[]).Select(b => (int)b));
                    }
                }
                else if (Data is ushort[])
                {
                    if (groupelem == DICOMTags.PixelData)
                    {
                        // Don't bother
                    }
                    else
                    {
                        obj["data"] = new JArray(Data as ushort[]);
                    }
                }
                else
                {
                    // Should be a simple type that serializes nicely, a basic string, or a list of base types
                    obj["data"] = new JValue(Data);
                }
            }
            return obj;
        }

        /// <summary>
        /// Read-only DICOM Group Tag
        /// </summary>
        public ushort Group { get { return group; } }
        /// <summary>
        /// Read-only DICOM Element Tag
        /// </summary>
        public ushort Elem { get { return elem; } }
        /// <summary>
        /// Read-only DICOM GroupElement combo Tag (compare to <see cref="DICOMTags"/> values)
        /// </summary>
        public uint Tag { get { return groupelem; } }
        /// <summary>
        /// Returns the parent sequence item, if it is a child of a sequence element.  If this is a top level element, the parent element will be null.
        /// </summary>
        public SQItem ParentSQItem { get { return parentSQItem; } }

        /// <summary>
        /// Read or set the internal data of the element
        /// </summary>
        abstract public object Data { get; set; }

        /// <summary>
        /// Returns the length of all the data contained within this Element
        /// </summary>
        /// <param name="explicitVR">Whether to return the data length as if it were going to be used in an Explicit VR syntax or not.  Only
        /// used by SQ elements, since they have child elements that may need to write out VR tags that occupy more space.</param>
        /// <returns>The total size (in bytes) of the contained data.</returns>
        abstract public uint GetDataLength(bool explicitVR);

        internal uint GetLength(bool explicitVR)
        {
            if (explicitVR)
            {
                if (VR == "UT" || VR == "UN" || VR == "OB" || VR == "OW" || VR == "SQ")
                    return 12 + GetDataLength(explicitVR);
                else
                    return 8 + GetDataLength(explicitVR);
            }
            else
                return 8 + GetDataLength(explicitVR);
        }

        /// <summary>
        /// The byte position in the source stream or file that this element was read from
        /// </summary>
        public long ReadPosition
        {
            get;
            internal set;
        }

        /// <summary>
        /// The byte position that this element was last written to
        /// </summary>
        public long WritePosition
        {
            get;
            internal set;
        }

        /// <summary>
        /// The Value Representation of the Element
        /// </summary>
        abstract public string VR { get; }
        /// <summary>
        /// The Value Representation of the Element in 2-byte (ushort) format
        /// </summary>
        abstract public ushort VRShort { get; }

        /// <summary>
        /// Gets a string representation of the internal data of the element
        /// </summary>
        abstract public string Display { get; }

        /// <summary>
        /// Gets a description of the DICOM tag (if it's known -- must be a documented DICOM tag, otherwise it returns "Unknown")
        /// </summary>
        public string Description
        {
            get
            {
                DataDictionaryElement dde = DataDictionary.LookupElement(groupelem);
                if (dde != null)
                    return dde.Description;
                else
                    return "Unknown";
            }
        }

        /// <summary>
        /// Internal storage of group/element
        /// </summary>
        protected ushort group, elem;
        /// <summary>
        /// Internal storage of group/element
        /// </summary>
        protected uint groupelem;
        /// <summary>
        /// Internal storage of the parent SQItem
        /// </summary>
        protected SQItem parentSQItem;


        internal void Write(SwappableBinaryWriter bw, ILogger logger, TransferSyntax transferSyntax)
        {
            //Group/Element
            bw.Write(group);
            bw.Write(elem);

            // Group 0 is always little-endian implicit vr, group 2 is always little-endian explicit vr
            if ((transferSyntax.ExplicitVR && group != 0) || group == 2)
            {
                //Shortcut write OB always for image data, even if it's a sequence
                if (Group == 0x7FE0 && Elem == 0x0010)
                    bw.Write(DICOMElementOB.vrshort);
                else
                    bw.Write(VRShort);

                if (VR == "UT" || VR == "UN" || VR == "OB" || VR == "OW" || VR == "SQ")
                {
                    //2 reserved bytes
                    bw.Write((short)0);

                    //4 Length
                    if (Group == 0x7FE0 && Elem == 0x0010 && VR == "SQ")
                        bw.Write((uint)0xFFFFFFFF);
                    else
                        bw.Write(GetDataLength(true));
                }
                else
                {
                    //2 Length
                    bw.Write((ushort)GetDataLength(true));
                }
            }
            else
            {
                //4 Length
                if (Group == 0x7FE0 && Elem == 0x0010 && VR == "SQ")
                    bw.Write((uint)0xFFFFFFFF);
                else
                    bw.Write(GetDataLength(false));
            }

            this.WriteData(bw, logger, transferSyntax);
        }

        //parseLen returns total bytes read
        internal static DICOMElement Parse(ushort group, ushort elem, ILogger logger, TransferSyntax transferSyntax, SwappableBinaryReader br, SQItem parentSQ, bool skipOverData, out uint parseLen)
        {
            ushort vr;
            uint len;
            uint headerBytes = 0;

            // Group 0 is always little-endian implicit vr, group 2 is always little-endian explicit vr
            if ((transferSyntax.ExplicitVR && group != 0) || group == 2)
            {
                vr = br.ReadUInt16MSB();
                headerBytes += 2;

                if (vr == DICOMElementUT.vrshort || vr == DICOMElementUN.vrshort || vr == DICOMElementOB.vrshort
                    || vr == DICOMElementOW.vrshort || vr == DICOMElementSQ.vrshort)
                {
                    //next 2 bytes reserved
                    br.ReadUInt16();
                    headerBytes += 2;

                    //len is next 4
                    len = br.ReadUInt32();
                    headerBytes += 4;
                }
                else
                {
                    //len is next 2
                    len = br.ReadUInt16();
                    headerBytes += 2;
                }
            }
            else
            {
                //Look up VR in the dictionary
                DataDictionaryElement dde = DataDictionary.LookupElement(group, elem);
                if (dde != null)
                {
                    //Found it.
                    vr = dde.vrshort;
                }
                else
                {
                    //No idea... OB!
                    vr = DICOMElementOB.vrshort;
                }

                //Length will always be 32-bit here
                len = br.ReadUInt32();
                headerBytes += 4;
            }

            DICOMElement ret = null;
            //Is it a compressed weirdo image data sequence that's actually an SQ?
            if (len == 0xFFFFFFFF)
                ret = new DICOMElementSQ(group, elem);
            else
                ret = CreateByVR(group, elem, vr);

            ret.parentSQItem = parentSQ;

            //Parse internal Data
            parseLen = headerBytes;

            try
            {
                // Can't skip over the length if it's not explicit
                if (skipOverData && len != 0xFFFFFFFF)
                {
                    parseLen += len;
                    br.BaseStream.Seek(len, SeekOrigin.Current);
                }
                else
                {
                    parseLen += ret.ParseData(br, logger, len, transferSyntax);
                }
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, "Exception in ParseData(grp=" + group + ", elem=" + elem + ", len=" + len + "): " + e.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Returns a new DICOMElement based on the listed group and element.  It looks up the element in the data dictionary
        /// to figure out the correct VR for the tag, and creates the proper DICOM Element type.
        /// </summary>
        /// <param name="group">DICOM Group Tag</param>
        /// <param name="elem">DICOM Element Tag</param>
        /// <returns>A DICOMElement encapsulating the specified Group and Element</returns>
        public static DICOMElement CreateFromGroupElem(ushort group, ushort elem)
        {
            DataDictionaryElement dde = DataDictionary.LookupElement(group, elem);
            if (dde != null)
                return CreateByVR(dde.Group, dde.Elem, dde.vrshort);
            else
                return CreateByVR(group, elem, DICOMElementOB.vrshort);
        }

        /// <summary>
        /// Returns a new DICOMElement based on the listed tag from <see cref="DICOMTags"/>
        /// </summary>
        /// <param name="tag">A DICOM Tag from <see cref="DICOMTags"/></param>
        /// <returns>A new DICOMElement encapsulating the specified Tag</returns>
        public static DICOMElement CreateFromTag(uint tag)
        {
            DataDictionaryElement dde = DataDictionary.LookupElement(tag);
            if (dde != null)
                return CreateByVR(dde.Group, dde.Elem, dde.vrshort);
            else
                return CreateByVR((ushort)(tag << 16), (ushort)tag, DICOMElementOB.vrshort);
        }

        /// <summary>
        /// Returns a new DICOMElement object of the correct VR, using the specified Group and Element
        /// </summary>
        /// <param name="group">A DICOM Group Tag</param>
        /// <param name="elem">A DICOM Element Tag</param>
        /// <param name="vr">A DICOM Value Representation (VR), a two character string</param>
        /// <returns>A new DICOMElement encapsulating theh specified group, element, and VR.</returns>
        public static DICOMElement CreateByVR(ushort group, ushort elem, ushort vr)
        {
            DICOMElement ret = null;
            if (vr == DICOMElementAE.vrshort) ret = new DICOMElementAE(group, elem);
            else if (vr == DICOMElementAS.vrshort) ret = new DICOMElementAE(group, elem);
            else if (vr == DICOMElementAT.vrshort) ret = new DICOMElementAT(group, elem);
            else if (vr == DICOMElementCS.vrshort) ret = new DICOMElementCS(group, elem);
            else if (vr == DICOMElementDA.vrshort) ret = new DICOMElementDA(group, elem);
            else if (vr == DICOMElementDS.vrshort) ret = new DICOMElementDS(group, elem);
            else if (vr == DICOMElementDT.vrshort) ret = new DICOMElementDT(group, elem);
            else if (vr == DICOMElementFD.vrshort) ret = new DICOMElementFD(group, elem);
            else if (vr == DICOMElementFL.vrshort) ret = new DICOMElementFL(group, elem);
            else if (vr == DICOMElementIS.vrshort) ret = new DICOMElementIS(group, elem);
            else if (vr == DICOMElementLO.vrshort) ret = new DICOMElementLO(group, elem);
            else if (vr == DICOMElementLT.vrshort) ret = new DICOMElementLT(group, elem);
            else if (vr == DICOMElementOB.vrshort) ret = new DICOMElementOB(group, elem);
            else if (vr == DICOMElementOW.vrshort) ret = new DICOMElementOW(group, elem);
            else if (vr == DICOMElementPN.vrshort) ret = new DICOMElementPN(group, elem);
            else if (vr == DICOMElementSH.vrshort) ret = new DICOMElementSH(group, elem);
            else if (vr == DICOMElementSL.vrshort) ret = new DICOMElementSL(group, elem);
            else if (vr == DICOMElementSQ.vrshort) ret = new DICOMElementSQ(group, elem);
            else if (vr == DICOMElementSS.vrshort) ret = new DICOMElementSS(group, elem);
            else if (vr == DICOMElementST.vrshort) ret = new DICOMElementST(group, elem);
            else if (vr == DICOMElementTM.vrshort) ret = new DICOMElementTM(group, elem);
            else if (vr == DICOMElementUI.vrshort) ret = new DICOMElementUI(group, elem);
            else if (vr == DICOMElementUL.vrshort) ret = new DICOMElementUL(group, elem);
            else if (vr == DICOMElementUN.vrshort) ret = new DICOMElementUN(group, elem);
            else if (vr == DICOMElementUS.vrshort) ret = new DICOMElementUS(group, elem);
            else if (vr == DICOMElementUT.vrshort) ret = new DICOMElementUT(group, elem);
            else ret = new DICOMElementOB(group, elem);    //fall back to OB
            return ret;
        }

        private static HashSet<ushort> vrLookup;
        static DICOMElement()
        {
            vrLookup = new HashSet<ushort>();
            vrLookup.Add(DICOMElementAE.vrshort);
            vrLookup.Add(DICOMElementAS.vrshort);
            vrLookup.Add(DICOMElementAT.vrshort);
            vrLookup.Add(DICOMElementCS.vrshort);
            vrLookup.Add(DICOMElementDA.vrshort);
            vrLookup.Add(DICOMElementDS.vrshort);
            vrLookup.Add(DICOMElementDT.vrshort);
            vrLookup.Add(DICOMElementFD.vrshort);
            vrLookup.Add(DICOMElementFL.vrshort);
            vrLookup.Add(DICOMElementIS.vrshort);
            vrLookup.Add(DICOMElementLO.vrshort);
            vrLookup.Add(DICOMElementLT.vrshort);
            vrLookup.Add(DICOMElementOB.vrshort);
            vrLookup.Add(DICOMElementOW.vrshort);
            vrLookup.Add(DICOMElementPN.vrshort);
            vrLookup.Add(DICOMElementSH.vrshort);
            vrLookup.Add(DICOMElementSL.vrshort);
            vrLookup.Add(DICOMElementSQ.vrshort);
            vrLookup.Add(DICOMElementSS.vrshort);
            vrLookup.Add(DICOMElementST.vrshort);
            vrLookup.Add(DICOMElementTM.vrshort);
            vrLookup.Add(DICOMElementUI.vrshort);
            vrLookup.Add(DICOMElementUL.vrshort);
            vrLookup.Add(DICOMElementUN.vrshort);
            vrLookup.Add(DICOMElementUS.vrshort);
            vrLookup.Add(DICOMElementUT.vrshort);
        }
    }
}
