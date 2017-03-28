using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Elements;
using System.IO;

namespace DICOMSharp.Data.Compression
{
    /// <summary>
    /// This class is a worker class to handle compression/decompression of DICOMData sets.  The only public methods are provided
    /// to directly check if your desired compression/decompression types are currently supported.
    /// </summary>
    public class CompressionWorker
    {
        private CompressionWorker() { }

        /// <summary>
        /// Checks to see if we currently support decompressing from the requested compression type.
        /// </summary>
        /// <param name="compression">The compression type to check</param>
        /// <returns>Whether or not it is supported.  This is not related to whether <u>compression</u> of this method is supported or not.</returns>
        public static bool SupportsDecompression(CompressionInfo compression)
        {
            if (compression == CompressionInfo.JPIP || compression == CompressionInfo.MPEG2)
                return false;
            return true;
        }

        /// <summary>
        /// Checks to see if we currently support compressing to the requested compression type.
        /// </summary>
        /// <param name="compression">The compression type to check</param>
        /// <returns>Whether or not it is supported.  This is not related to whether <u>decompression</u> of this method is supported or not.</returns>
        public static bool SupportsCompression(CompressionInfo compression)
        {
            if (compression == CompressionInfo.JPIP || compression == CompressionInfo.MPEG2 || compression == CompressionInfo.RLE)
                return false;
            return true;
        }

        internal static bool Uncompress(DICOMData data)
        {
            if (data.TransferSyntax.Compression != CompressionInfo.None)
            {
                if (!data.Elements.ContainsKey(DICOMTags.PixelData))
                    return false;

                //Compressed in some way.  Pull the inner data out...
                DICOMElement pixelDataElem = data.Elements[DICOMTags.PixelData];

                ushort bitsAllocated = (ushort)data.Elements[DICOMTags.BitsAllocated].Data;

                //Get other image metrics
                ushort imWidth = (ushort)data.Elements[DICOMTags.ImageWidth].Data;
                ushort imHeight = (ushort)data.Elements[DICOMTags.ImageHeight].Data;

                bool hasNumFramesTag = data.Elements.ContainsKey(DICOMTags.NumberOfFrames);
                int numFrames = 1;
                if (hasNumFramesTag && int.TryParse((string)data.Elements[DICOMTags.NumberOfFrames].Data, out numFrames)) { }
                int samplesPerPixel = (data.Elements.ContainsKey(DICOMTags.SamplesPerPixel) ? (int)(ushort)data.Elements[DICOMTags.SamplesPerPixel].Data : 1);
                int planarConfiguration = (data.Elements.ContainsKey(DICOMTags.PlanarConfiguration) ? (int)(ushort)data.Elements[DICOMTags.PlanarConfiguration].Data : 0);

                string photoInterp = (string)data[DICOMTags.PhotometricInterpretation].Data;
                bool ybr = (photoInterp == "YBR_FULL" || photoInterp == "YBR_FULL_422");

                if (pixelDataElem is DICOMElementOB)
                {
                    //Single frame, non-encapsulated.  Pull the data out of the OB element.
                    byte[] inData = (byte[])((DICOMElementOB)pixelDataElem).Data;

                    byte[] uncompressedData = UncompressData(inData, data.TransferSyntax.Compression, imWidth, imHeight, bitsAllocated, samplesPerPixel, planarConfiguration, ybr);
                    if (uncompressedData != null)
                    {
                        //Set new data and change transfer syntax.  Good to go!
                        pixelDataElem.Data = uncompressedData;
                        data.TransferSyntax = TransferSyntaxes.ExplicitVRLittleEndian;
                    }
                }
                else if (pixelDataElem is DICOMElementSQ)
                {
                    //Encapsulated, and potentially multiframe...
                    DICOMElementSQ sqElem = (DICOMElementSQ)pixelDataElem;

                    bool didUncompress = false;

                    if (!hasNumFramesTag && sqElem.Items.Count > 1)
                    {
                        // Image has multiple sequence items, but is not multiframe -- must require concatenation.
                        // In the past this path was used for JPEGLS, but I think this is the correct IF check for it.

                        int totalLen = 0;
                        for (int i = 1; i < sqElem.Items.Count; i++)
                            totalLen += sqElem.Items[i].EncapsulatedImageData.Length;

                        byte[] compressedData = new byte[totalLen];
                        int bytePtr = 0;
                        for (int i = 1; i < sqElem.Items.Count; i++)
                        {
                            SQItem sqItem = sqElem.Items[i];
                            Array.Copy(sqItem.EncapsulatedImageData, 0, compressedData, bytePtr, sqItem.EncapsulatedImageData.Length);
                            bytePtr += sqItem.EncapsulatedImageData.Length;
                        }

                        byte[] uncompressedData = UncompressData(compressedData, data.TransferSyntax.Compression, imWidth, imHeight, bitsAllocated, samplesPerPixel, planarConfiguration, ybr);
                        if (uncompressedData != null)
                        {
                            //Remove image encapsulation

                            //Make a new image data element for the data!
                            DICOMElementOB newElem = new DICOMElementOB(0x7FE0, 0x0010);
                            newElem.Data = uncompressedData;

                            //Store it back to the DICOMData, overwriting the original...
                            data[DICOMTags.PixelData] = newElem;

                            didUncompress = true;
                        }
                    }
                    else
                    {
                        //Iterate through frames.  Skip the first pseudo-frame since it's just a lookup entry.
                        for (int i = 1; i < sqElem.Items.Count; i++)
                        {
                            SQItem sqItem = sqElem.Items[i];
                            byte[] inData = null;
                            if (sqItem.IsEncapsulatedImage)
                            {
                                inData = sqItem.EncapsulatedImageData;
                            }
                            else if (sqItem.Elements.Count > 0)
                            {
                                DICOMElement innerElem = sqItem.Elements[0];
                                if (innerElem != null && innerElem is DICOMElementOB)
                                    inData = (byte[])((DICOMElementOB)innerElem).Data;
                            }

                            //Process frame
                            if (inData != null)
                            {
                                byte[] uncompressedData = UncompressData(inData, data.TransferSyntax.Compression, imWidth, imHeight, bitsAllocated, samplesPerPixel, planarConfiguration, ybr);
                                if (uncompressedData != null)
                                {
                                    if (numFrames > 1)
                                    {
                                        //Image is multiframe, so just update the encapsulated data to have the new frame
                                        sqItem.EncapsulatedImageData = uncompressedData;

                                        didUncompress = true;
                                    }
                                    else
                                    {
                                        //Remove image encapsulation

                                        //Make a new image data element for the data!
                                        DICOMElementOB newElem = new DICOMElementOB(0x7FE0, 0x0010);
                                        newElem.Data = uncompressedData;

                                        //Store it back to the DICOMData, overwriting the original...
                                        data[DICOMTags.PixelData] = newElem;

                                        didUncompress = true;
                                    }
                                }
                            }
                        }
                    }

                    if (didUncompress)
                    {
                        //Set the transfer syntax to uncompressed!
                        data.TransferSyntax = TransferSyntaxes.ExplicitVRLittleEndian;
                    }
                }

                if (samplesPerPixel == 3)
                {
                    //check for needed colorspace conversion
                    if (ybr)
                        data[DICOMTags.PhotometricInterpretation].Data = "RGB";
                }
            }

            return true;
        }

        internal static bool Compress(DICOMData data, TransferSyntax newSyntax)
        {
            if (data.TransferSyntax.Compression != CompressionInfo.None)
                return false;
            if (!SupportsCompression(data.TransferSyntax.Compression))
                return false;

            //Get the pixel data element, and make sure it's OB
            if (!data.Elements.ContainsKey(DICOMTags.PixelData))
            {
                return false;
            }
            DICOMElement testElem = data.Elements[DICOMTags.PixelData];

            //Figure out bit depth
            DICOMElement bitsAllocatedElem = data.Elements[DICOMTags.BitsAllocated];
            int bitsAllocated = int.Parse(bitsAllocatedElem.Display);

            //Prepare for compression
            ushort imWidth = (ushort)data.Elements[DICOMTags.ImageWidth].Data;
            ushort imHeight = (ushort)data.Elements[DICOMTags.ImageHeight].Data;
            int frameSize = imWidth * imHeight * (bitsAllocated / 8);

            bool multiFrame = (data.Elements.ContainsKey(DICOMTags.NumberOfFrames));
            int numFrames = 1;
            if (multiFrame && int.TryParse((string)data.Elements[DICOMTags.NumberOfFrames].Data, out numFrames)) { }
            int samplesPerPixel = (data.Elements.ContainsKey(DICOMTags.SamplesPerPixel) ? (int)(ushort)data.Elements[DICOMTags.SamplesPerPixel].Data : 1);
            int planarConfiguration = (data.Elements.ContainsKey(DICOMTags.PlanarConfiguration) ? (int)(ushort)data.Elements[DICOMTags.PlanarConfiguration].Data : 0);

            //Quick type test to make sure encapsulation is correct
            if (multiFrame && (testElem is DICOMElementSQ) && (((List<SQItem>)testElem.Data).Count < numFrames + 1))
                return false;
            if (!multiFrame && !(testElem is DICOMElementOB))
                return false;

            //Encapsulate the new element
            DICOMElementSQ newData = new DICOMElementSQ(0x7FE0, 0x0010);

            //Create the pointer lookup
            SQItem lookupItem = new SQItem(newData);
            newData.Items.Add(lookupItem);
            byte[] lookupData = new byte[4 * numFrames];
            lookupItem.EncapsulatedImageData = lookupData;

            int dataPtr = 0;
            for (int i = 0; i < numFrames; i++)
            {
                //Add pointer to lookup
                Array.Copy(BitConverter.GetBytes(dataPtr), 0, lookupData, 4 * i, 4);

                byte[] inData;
                int startPtr;
                if (testElem is DICOMElementSQ)
                {
                    inData = ((List<SQItem>)testElem.Data)[i + 1].EncapsulatedImageData;
                    startPtr = 0;
                }
                else
                {
                    inData = (byte[])testElem.Data;
                    startPtr = i * frameSize;
                }

                //Compress data and add to new encapsulated item
                byte[] outData = null;
                unsafe
                {
                    if (newSyntax.Compression == CompressionInfo.JPEG2000)
                    {
                        fixed (byte* pInData = inData)
                        {
                            int lenOut = 0;
                            byte* dataOut = (byte*)CompressJ2K((IntPtr)(pInData + startPtr), bitsAllocated, imWidth, imHeight, samplesPerPixel, planarConfiguration, ref lenOut);
                            outData = new byte[lenOut];
                            Marshal.Copy((IntPtr)dataOut, outData, 0, lenOut);
                            FreePtr((IntPtr)dataOut);
                        }
                    }
                    else if (newSyntax.Compression == CompressionInfo.JPEGLossless || newSyntax.Compression == CompressionInfo.JPEGLossy)
                    {
                        fixed (byte* pInData = inData)
                        {
                            int compressionMode = 0;    //0 = baseline, 1 = extended sequential, 2 = spectralselec, 3 = progressive, 4 = lossless
                            int firstOrder = 1;         //1 = first order, 0 = not?  0 seems to be rejected.
                            int pointTrans = 0;         //0 is the default point transformation... don't know how to use this...

                            if (newSyntax == TransferSyntaxes.JPEGBaselineProcess1) compressionMode = 0;   //baseline
                            else if (newSyntax == TransferSyntaxes.JPEGExtendedProcess24) compressionMode = 1;   //extended?
                            else if (newSyntax == TransferSyntaxes.JPEGLosslessNonHierarchicalFirstOrderPredictionProcess14) { compressionMode = 4; firstOrder = 1; pointTrans = 0; }   //lossless first order
                            else if (newSyntax == TransferSyntaxes.JPEGLosslessNonHierarchicalProcess14) { compressionMode = 4; firstOrder = 1; pointTrans = 0; }   //lossless non-first order

                            int lenOut = 0;
                            byte* dataOut = (byte*)CompressJPEG((IntPtr)(pInData + startPtr), bitsAllocated, imWidth, imHeight, samplesPerPixel, planarConfiguration, compressionMode, firstOrder, pointTrans, ref lenOut);
                            outData = new byte[lenOut];
                            Marshal.Copy((IntPtr)dataOut, outData, 0, lenOut);
                            FreePtr((IntPtr)dataOut);
                        }
                    }
                    else if (newSyntax.Compression == CompressionInfo.JPEGLSLossless || newSyntax.Compression == CompressionInfo.JPEGLSLossy)
                    {
                        fixed (byte* pInData = inData)
                        {
                            int compressionMode = (newSyntax.Compression == CompressionInfo.JPEGLSLossless) ? 0 : 1;    //allowed difference: 0 = lossless, >=1 = lossy -- >1 seems to crash it...

                            int lenOut = 0;
                            byte* dataOut = (byte*)CompressJPEGLS((IntPtr)(pInData + startPtr), bitsAllocated, imWidth, imHeight, samplesPerPixel, planarConfiguration, compressionMode, ref lenOut);
                            outData = new byte[lenOut];
                            Marshal.Copy((IntPtr)dataOut, outData, 0, lenOut);
                            FreePtr((IntPtr)dataOut);
                        }
                    }
                }

                if (outData == null)
                    return false;

                //Add new image data sequence item
                SQItem item = new SQItem(newData);
                item.EncapsulatedImageData = outData;
                newData.Items.Add(item);

                //Update pointer for lookup
                dataPtr += outData.Length;
            }

            data[DICOMTags.PixelData] = newData;

            data.TransferSyntax = newSyntax;

            if (samplesPerPixel == 3)
            {
                //check for needed colorspace conversion
                string photoInterp = (string)data[DICOMTags.PhotometricInterpretation].Data;
                if (photoInterp == "RGB")
                    data[DICOMTags.PhotometricInterpretation].Data = "YBR_FULL_422";
            }

            return true;
        }

        private static byte[] UncompressData(byte[] inData, CompressionInfo compressionInfo, int width, int height, int bitsAllocated, int samplesPerPixel, int planarConfiguration, bool ybr)
        {
            unsafe
            {
                if (compressionInfo == CompressionInfo.JPEG2000 || compressionInfo == CompressionInfo.JPEGLossless || compressionInfo == CompressionInfo.JPEGLossy
                    || compressionInfo == CompressionInfo.JPEGLSLossless || compressionInfo == CompressionInfo.JPEGLSLossy) //jpeg-ls seems to work too. neat.
                {
                    fixed (byte* pInData = inData)
                    {
                        int lenOut = 0;
                        byte* dataOut = null;
                        if (compressionInfo == CompressionInfo.JPEG2000)
                            dataOut = (byte*)UncompressJ2K((IntPtr)pInData, inData.Length, bitsAllocated, width, height, samplesPerPixel, planarConfiguration, ref lenOut);
                        else if (compressionInfo == CompressionInfo.JPEGLSLossless || compressionInfo == CompressionInfo.JPEGLSLossy)
                            dataOut = (byte*)UncompressJPEGLS((IntPtr)pInData, inData.Length, bitsAllocated, width, height, samplesPerPixel, planarConfiguration, ref lenOut);
                        else
                            dataOut = (byte*)UncompressJPEG((IntPtr)pInData, inData.Length, bitsAllocated, width, height, samplesPerPixel, planarConfiguration, ybr, ref lenOut);

                        if (dataOut == null)
                            return null;

                        byte[] destArray = new byte[lenOut];
                        Marshal.Copy((IntPtr)dataOut, destArray, 0, lenOut);
                        FreePtr((IntPtr)dataOut);
                        return destArray;
                    }
                }
                if (compressionInfo == CompressionInfo.RLE) //PS 3.5 Annex G
                {
                    // TODO: Do we care about planar config?

                    //Pull out RLE segment header
                    int numSegments = BitConverter.ToInt32(inData, 0);
                    int[] segmentOffsets = new int[numSegments];
                    for (int i = 0; i < numSegments; i++)
                        segmentOffsets[i] = BitConverter.ToInt32(inData, 4 + i * 4);

                    int segmentBytes = width * height * (bitsAllocated / 8);

                    //Generate a new stream
                    MemoryStream outStream = new MemoryStream(numSegments * segmentBytes);

                    for (int i = 0; i < numSegments; i++)
                    {
                        int endOffset = (i < numSegments - 1) ? segmentOffsets[i + 1] : inData.Length;

                        int ptr = segmentOffsets[i];
                        while (ptr < endOffset)
                        {
                            sbyte controlChar = (sbyte)inData[ptr++];
                            if (controlChar == -128)
                            {
                                //Do nothing
                            }
                            else if (controlChar >= 0 && controlChar <= 127)
                            {
                                //Output the next n+1 bytes literally
                                if (ptr + (int)controlChar + 1 < endOffset)
                                    outStream.Write(inData, ptr, (int)controlChar + 1);
                                ptr += (int)controlChar + 1;
                            }
                            else //-1 to -127
                            {
                                //Output the next byte -n+1 times
                                if (ptr + 1 < endOffset)
                                    for (int h = 0; h < 1 - (int)controlChar; h++)
                                        outStream.WriteByte(inData[ptr]);
                                ptr++;
                            }
                        }

                        // Sometimes it seems to stop early, so pad with zeroes.
                        long extraBytes = (i + 1) * segmentBytes - outStream.Length;
                        while (extraBytes-- > 0)
                        {
                            outStream.WriteByte(0);
                        }
                    }

                    var buffer = outStream.GetBuffer();
                    if (ybr)
                    {
                        // from http://www.fourcc.org/fccyvrgb.php
                        for (long i = 0; i < segmentBytes; i++)
                        {
                            byte Y = buffer[i];
                            int Cb = (int)buffer[i + segmentBytes] - 128;
                            int Cr = (int)buffer[i + 2 * segmentBytes] - 128;

                            int R = (int) (Y + 1.403 * Cr);
                            int G = (int) (Y - 0.344 * Cb - 0.714 * Cr);
                            int B = (int) (Y + 1.770 * Cb);

                            buffer[i] = (byte)(R < 0 ? 0 : (R > 255 ? 255 : R));
                            buffer[i + segmentBytes] = (byte)(G < 0 ? 0 : (G > 255 ? 255 : G));
                            buffer[i + 2 * segmentBytes] = (byte)(B < 0 ? 0 : (B > 255 ? 255 : B));
                        }
                    }
                    return buffer;
                }
            }
            return null;
        }

        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?UncompressJPEG@@YAPAEPAEHHHHHH_NPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr UncompressJPEG(IntPtr dataIn, int length, int bitsAllocated, int width, int height,
            int samplesPerPixel, int planarConfiguration, bool ybr, ref int lenOut);
        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?UncompressJPEGLS@@YAPAEPAEHHHHHHPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr UncompressJPEGLS(IntPtr dataIn, int length, int bitsAllocated, int width, int height,
            int samplesPerPixel, int planarConfiguration, ref int lenOut);
        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?UncompressJ2K@@YAPAEPAEHHHHHHPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr UncompressJ2K(IntPtr dataIn, int length, int bitsAllocated, int width, int height,
            int samplesPerPixel, int planarConfiguration, ref int lenOut);

        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?CompressJPEG@@YAPAEPAEHHHHHHHHPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CompressJPEG(IntPtr dataIn, int bitsAllocated, int width, int height, int samplesPerPixel,
            int planarConfiguration, int compressMode, int firstOrder, int pointTrans, ref int lenOut);
        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?CompressJPEGLS@@YAPAEPAEHHHHHHPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CompressJPEGLS(IntPtr dataIn, int bitsAllocated, int width, int height, int samplesPerPixel,
            int planarConfiguration, int compressMode, ref int lenOut);
        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?CompressJ2K@@YAPAEPAEHHHHHPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CompressJ2K(IntPtr dataIn, int bitsAllocated, int width, int height, int samplesPerPixel,
            int planarConfiguration, ref int lenOut);

        [DllImport("DICOMSharpJPEGCompression.dll", EntryPoint = "?FreePtr@@YAXPAE@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreePtr(IntPtr ptrToFree);
    }
}
