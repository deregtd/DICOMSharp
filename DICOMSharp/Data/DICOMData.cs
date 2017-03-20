using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using DICOMSharp.Data.Elements;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Compression;
using DICOMSharp.Util;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System;
using DICOMSharp.Logging;

namespace DICOMSharp.Data
{
    /// <summary>
    /// The DICOMData class is the core of DICOMSharp.  It contains and organizes all information for both DICOM file management and for DICOM
    /// communication.  The class contains methods to load (<see cref="ParseFile"/>) and write (<see cref="WriteFile"/>) DICOM files from disk
    /// and provides access to all of the DICOMElements contained within it via the <see cref="Elements"/> property.
    /// </summary>
    public class DICOMData
    {
        /// <summary>
        /// Initializes a new empty instance of the <see cref="DICOMData"/> class.
        /// </summary>
        public DICOMData()
        {
            Elements = new SortedDictionary<uint, DICOMElement>();
            TransferSyntax = TransferSyntaxes.ImplicitVRLittleEndian;
        }

        /// <summary>
        /// Parses/loads a DICOM file's contents into the DICOMData structure.
        /// </summary>
        /// <param name="filePath">The full path of the file to load.</param>
        /// <param name="loadImageData">If set to <c>true</c>, it will load the entire file.
        /// If set to <c>false</c>, it will skip the image data.
        /// This is useful for speedy reading of headers of image data, when you don't care about the actual pixel data.
        /// Be aware that if this is set to false and you try to save the image out again or manipulate it in any way,
        /// there will be no image data at all -- there is no delayed load if you ask for it.</param>
        /// <param name="logger">Logger to use while parsing the file.</param>
        /// <returns>Returns <c>true</c> if parsing was successful, or <c>false</c> if an error was encountered.</returns>
        public bool ParseFile(string filePath, bool loadImageData, ILogger logger)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
                throw new FileNotFoundException();

            FileStream stream = fi.OpenRead();

            //Test for DICM Header
            stream.Seek(128, SeekOrigin.Begin);
            byte[] DICOMtest = new byte[4];
            stream.Read(DICOMtest, 0, 4);
            if (Encoding.ASCII.GetString(DICOMtest) == "DICM")
            {
                //Has it!  Start reading from here (132 bytes in).
            }
            else
            {
                //Nope!  Go back to start!
                stream.Seek(0, SeekOrigin.Begin);
            }

            //do implicit as a default -- it will figure out explicit later if it turns out to be...
            bool ret = ParseStream(stream, TransferSyntaxes.ImplicitVRLittleEndian, true, loadImageData, logger);

            stream.Close();
            return ret;
        }

        internal bool ParseStream(Stream stream, TransferSyntax transferSyntax, bool allowSyntaxChanges, bool loadImageData, ILogger logger)
        {
            SwappableBinaryReader sr = new SwappableBinaryReader(stream);
            long readPosition = stream.Position;

            bool inGroup2 = false;

            if (transferSyntax.MSBSwap) //this should be applicable...
                sr.ToggleSwapped();
            
            TransferSyntax parsedSyntax = null;

            while (readPosition + 8 < stream.Length)
            {
                //Read in group/element info
                ushort group = sr.ReadUInt16();
                ushort elem = sr.ReadUInt16();

                //Leaving the header?
                if (inGroup2 != (group == 2))
                {
                    if (transferSyntax.MSBSwap)
                        sr.ToggleSwapped();

                    inGroup2 = (group == 2);
                }

                //Stop loading if we're at image data and not supposed to read it
                var skipData = !loadImageData && group == 0x7FE0 && elem == 0x0010;

                //Make element
                uint outLen;
                DICOMElement nelem;
                try
                {
                    nelem = DICOMElement.Parse(group, elem, logger, inGroup2 ? true : transferSyntax.ExplicitVR, sr, null, skipData, out outLen);
                }
                catch (Exception e)
                {
                    logger.Log(LogLevel.Error, "Exception in DICOMElement.Parse: " + e.ToString());
                    return false;
                }

                //Debugging:
                //Console.WriteLine(nelem.Dump());

                //Store reading position in case it's useful later
                nelem.ReadPosition = readPosition;

                //Store element in lookup array
                Elements[nelem.Tag] = nelem;

                //Store transfer syntax change for after the header
                if (nelem.Tag == DICOMTags.TransferSyntaxUID)
                {
                    parsedSyntax = TransferSyntaxes.Lookup((string)nelem.Data);
                    if (allowSyntaxChanges)
                        transferSyntax = parsedSyntax;
                }

                //update read position pointer
                readPosition = stream.Position;
            }

            //Store whatever TS we ended up with
            TransferSyntax = (parsedSyntax != null) ? parsedSyntax : transferSyntax;

            return true;
        }

        /// <summary>
        /// Writes the contents of the DICOMData structure to a DICOM file.
        /// </summary>
        /// <param name="filePath">The full path of the file to write to, which will be overwritten if it already exists.</param>
        /// <param name="logger">Logger to use while writing out the file.</param>
        /// <returns>Returns <c>true</c> if writing was successful, or <c>false</c> if an error was encountered.</returns>
        public bool WriteFile(string filePath, ILogger logger)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.Exists)
                fi.Delete();
            else
                Directory.CreateDirectory(fi.DirectoryName);

            FileStream stream = fi.OpenWrite();

            var ret = this.WriteToStreamAsPart10File(stream, logger);
            stream.Close();

            return ret;
        }

        /// <summary>
        /// Writes the contents of the DICOMData structure to a stream as if it were being written to a file (explicit VR, little endian, with 132-byte header).
        /// </summary>
        /// <param name="stream">The stream to write the data to.</param>
        /// <param name="logger">Logger to use while writing out the file.</param>
        /// <returns>Returns <c>true</c> if writing was successful, or <c>false</c> if an error was encountered.</returns>
        public bool WriteToStreamAsPart10File(Stream stream, ILogger logger)
        {
            //Write header
            for (int i = 0; i < 128; i++)
                stream.WriteByte(0);

            byte[] header = Encoding.ASCII.GetBytes("DICM");
            stream.Write(header, 0, 4);

            // Write out important headers
            if (!Elements.ContainsKey(DICOMTags.MediaStorageSOPClassUID))
            {
                this[DICOMTags.MediaStorageSOPClassUID].Data = this[DICOMTags.SOPClassUID].Data;
            }
            if (!Elements.ContainsKey(DICOMTags.MediaStorageSOPInstanceUID))
            {
                this[DICOMTags.MediaStorageSOPInstanceUID].Data = this[DICOMTags.SOPInstanceUID].Data;
            }
            this[DICOMTags.TransferSyntaxUID].Data = TransferSyntax;

            return WriteStream(stream, logger, TransferSyntax, false);
        }

        /// <summary>
        /// Writes the contents of the DICOMData structure to a stream in a given set of pre- and post-header transfer syntaxes.
        /// </summary>
        /// <param name="stream">The stream to write the data to.</param>
        /// <param name="logger">Logger to use while writing out the file.</param>
        /// <param name="transferSyntax">The transfer syntax to use for writing out fields (other than group 2, which has special behavior).</param>
        /// <param name="isNetworkTransfer">For network transfers, you should never send group 2.</param>
        /// <returns>Returns <c>true</c> if writing was successful, or <c>false</c> if an error was encountered.</returns>
        public bool WriteStream(Stream stream, ILogger logger, TransferSyntax transferSyntax, bool isNetworkTransfer)
        {
            // Decompress/compress as needed to match the accepted transfer syntax.
            if (transferSyntax != this.TransferSyntax)
            {
                this.ChangeTransferSyntax(transferSyntax);
            }

            //Go through stream, make sure it has group lengths...
            //RxIMP: Optimize this to make it not recalc unless it has to!
            ushort lastGroup = 0xFFFF;
            uint sizeTally = 0;

            Dictionary<ushort, uint> sizesTemp = new Dictionary<ushort, uint>();

            foreach (DICOMElement elem in Elements.Values)
            {
                if (elem.Group != lastGroup)
                {
                    if (lastGroup != 0xFFFF)
                    {
                        //write out new group length tag!
                        sizesTemp[lastGroup] = sizeTally;
                        //this[(uint) lastGroup << 16].Data = sizeTally;
                        sizeTally = 0;
                    }
                    lastGroup = elem.Group;
                }

                if (elem.Elem != 0)
                {
                    // Group 2 is always explicit VR, if it's even sent (not for network transfers.)
                    sizeTally += elem.GetLength(elem.Group == 2 ? true : transferSyntax.ExplicitVR);
                }
            }
            //grab last group tag
            if (lastGroup != 0xFFFF)
                sizesTemp[lastGroup] = sizeTally;

            //Write out group lengths
            foreach (ushort group in sizesTemp.Keys)
                this[(uint)group << 16].Data = sizesTemp[group];

            SwappableBinaryWriter bw = new SwappableBinaryWriter(stream);
            if (transferSyntax.MSBSwap)
                bw.ToggleSwapped();

            bool inGroup2 = false;

            //Start writing tags!
            foreach (DICOMElement elem in Elements.Values)
            {
                //If we're moving in and out of group 2, change MSB swap as needed -- group 2 should always be little endian
                if (inGroup2 != (elem.Group == 2))
                {
                    if (transferSyntax.MSBSwap)
                    {
                        bw.ToggleSwapped();
                    }

                    inGroup2 = (elem.Group == 2);
                }

                //Store write position for later possible use
                elem.WritePosition = stream.Position;

                // Network transfers don't send group 2.
                if (elem.Group != 2 || !isNetworkTransfer)
                {
                    //Have the element write itself out.
                    elem.Write(bw, logger, elem.Group == 2 ? true : transferSyntax.ExplicitVR);
                }
            }

            return true;
        }

        /// <summary>
        /// Checks to see if we support both the decompression (if any) and compression (if any) to perform the transfer syntax change requested.
        /// If either step is not available, then this will return false, but you may still be able to do a decompression.  You can check that with
        /// the static methods on <see cref="DICOMSharp.Data.Compression.CompressionWorker"/>.
        /// </summary>
        /// <param name="newSyntax">The new transfer syntax you'd like to change to</param>
        /// <returns>Whether or not the complete change can be performed.</returns>
        public bool CanChangeTransferSyntax(TransferSyntax newSyntax)
        {
            if (!CompressionWorker.SupportsDecompression(this.TransferSyntax.Compression))
                return false;
            if (!CompressionWorker.SupportsCompression(newSyntax.Compression))
                return false;
            return true;
        }

        /// <summary>
        /// Changes the transfer syntax used by the DICOMData structure (compressed/uncompressed, endianness, implicit/explicit VR, etc.) to a new syntax.
        /// </summary>
        /// <param name="newSyntax">The new transfer syntax to change to.</param>
        /// <returns>Returns <c>true</c> if the change was successful, or <c>false</c> if an error was encountered.</returns>
        public bool ChangeTransferSyntax(TransferSyntax newSyntax)
        {
            if (!CanChangeTransferSyntax(newSyntax))
                return false;

            CompressionInfo oldCompression = TransferSyntax.Compression;
            CompressionInfo newCompression = newSyntax.Compression;
            if (oldCompression != newCompression)
            {
                //Is it compressed?
                if (oldCompression != CompressionInfo.None)
                {
                    //Attempt to uncompress
                    if (!this.Uncompress())
                        return false;
                }

                //Need to compress it?
                if (newCompression != CompressionInfo.None)
                {
                    //Attempt to compress
                    if (!CompressionWorker.Compress(this, newSyntax))
                        return false;
                }
            }

            //Compressionworker will do this...
            //TransferSyntax = newSyntax;
            return true;
        }

        /// <summary>
        /// Attempts to remove any compression from the image data and changes the Transfer Syntax to Explicit VR Little Endian.
        /// </summary>
        /// <returns>Returns <c>true</c> if the change was successful, or <c>false</c> if an error was encountered.</returns>
        public bool Uncompress()
        {
            return CompressionWorker.Uncompress(this);
        }

        /// <summary>
        /// Attempts to remove any identifying data from the DICOM file and replace them with repeatable one-way hashes.
        /// </summary>
        /// <returns>Returns <c>true</c> if the attempt was successful, or <c>false</c> if an error was encountered.</returns>
        public bool Anonymize()
        {
            var tags = new [] {
                DICOMTags.PatientName,
                DICOMTags.PatientID,
                DICOMTags.PatientBirthDate,
                DICOMTags.PatientAge,
                DICOMTags.PatientAddress,
                DICOMTags.InstitutionName,
                DICOMTags.InstitutionAddress,
                DICOMTags.ReferringPhysicianName,
                DICOMTags.ReferringPhysicianAddress
            };

            const string salt = "!@#%#fdsfs#@1182101dasimoeo1390";

            foreach (var tag in tags)
            {
                if (this.Elements.ContainsKey(tag))
                {
                    var elem = this.Elements[tag];
                    var preData = elem.Data as string;
                    if (preData != null)
                    {
                        elem.Data = StringUtil.MD5String(salt + preData);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Dumps the contents of the DICOMData structure to a string.  This is only really useful for debugging purposes.
        /// </summary>
        /// <returns>The return string contains a debug view of the entire DICOMData.</returns>
        public string Dump()
        {
            string outstr = "";
            foreach (DICOMElement elem in Elements.Values)
            {
                outstr += elem.Dump() + "\n";
                if (elem is DICOMElementSQ)
                    outstr += ((DICOMElementSQ)elem).GetDisplayContents(1);
            }
            return outstr;
        }

        /// <summary>
        /// Dumps the contents of the DICOMData structure to a JSON object, usually for use with web clients.
        /// </summary>
        public virtual JObject DumpJson()
        {
            var obj = new JObject();
            obj["elements"] = new JArray(Elements.Select(elem => elem.Value.DumpJson()));
            return obj;
        }

        /// <summary>
        /// Returns the current transfer syntax for the DICOMData set.  To change (compress/uncompress) the transfer syntax,
        /// use the <see cref="ChangeTransferSyntax"/> method.
        /// </summary>
        public TransferSyntax TransferSyntax
        {
            get
            {
                if (Elements.ContainsKey(DICOMTags.TransferSyntaxUID) &&
                    !string.IsNullOrWhiteSpace((string) Elements[DICOMTags.TransferSyntaxUID].Data))
                {
                    return TransferSyntaxes.Lookup((string) Elements[DICOMTags.TransferSyntaxUID].Data);
                }

                return transferSyntaxInt;
            }

            internal set
            {
                if (Elements.ContainsKey(DICOMTags.TransferSyntaxUID))
                {
                    Elements[DICOMTags.TransferSyntaxUID].Data = value;
                }

                transferSyntaxInt = value;
            }
        }
        private TransferSyntax transferSyntaxInt;

        /// <summary>
        /// This is a Dictionary containing all of the DICOM elements in the DICOMData.
        /// 
        /// They are keyed off the DICOM Tag of the element (<see cref="DICOMTags"/>).
        /// </summary>
        public SortedDictionary<uint, DICOMElement> Elements { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="DICOMSharp.Data.Elements.DICOMElement"/> with the specified tag.  This is basically a shortcut helper
        /// for the Elements property.
        /// 
        /// Note: If you attempt to retrieve a tag this way that doesn't currently exist in the DICOMData, it will be created and added to
        /// the Elements list.
        /// </summary>
        public DICOMElement this[uint tag]
        {
            get
            {
                //Return existing if it's here
                if (Elements.ContainsKey(tag))
                    return Elements[tag];

                //Create new element, add to storage, and return
                DICOMElement newElem = DICOMElement.CreateFromTag(tag);
                Elements[tag] = newElem;
                return newElem;
            }

            set
            {
                Elements[tag] = value;
            }
        }
    }
}
