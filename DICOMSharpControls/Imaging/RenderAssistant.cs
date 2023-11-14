using System;
using System.Collections.Generic;
using DICOMSharp.Data;
using System.Drawing;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Elements;
using DICOMSharp.Util;
using System.Drawing.Imaging;

namespace DICOMSharpControls.Imaging
{
    /// <summary>
    /// This class does simple rendering of a DICOM image.  This can be used for one-time rendering (i.e. preview
    /// generation), but it's mainly designed for ongoing rendering to a window.
    /// </summary>
    public class RenderAssistant
    {
        /// <summary>
        /// Create a new, empty, zero-size, RenderAssistant.
        /// </summary>
        public RenderAssistant()
        {
            data = null;
            size = Size.Empty;
            RenderedImage = null;

            ResetViewport();
        }

        /// <summary>
        /// Resize the output surface for the rendering.  This changes the size of the RenderedImage.
        /// Note: Calling this function does not automatically cause a redraw.
        /// </summary>
        /// <param name="newSize">The new size for the rendering.</param>
        public void Resize(Size newSize)
        {
            if (newSize != size)
            {
                size = newSize;
                RenderedImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            }
        }

        /// <summary>
        /// Resets the viewport to defaults.  This sets the zoom back to 1x (image fills panel) and centered.
        /// Note: Calling this function does not automatically cause a redraw.
        /// </summary>
        public void ResetViewport()
        {
            Zoom = 1.0f;
            CenterX = 0;
            CenterY = 0;
        }

        /// <summary>
        /// Sets/changes the DICOMData that the RenderAssistant will be rendering.  Important information about
        /// the image is pre-loaded for faster repeated rendering.
        /// Note: Calling this function does not automatically cause a redraw.
        /// You may also 
        /// </summary>
        /// <param name="newData">New DICOMData to draw from (may be null to clear parameters)</param>
        public void SetSource(DICOMData newData)
        {
            if (newData == data)
                return;
            data = newData;

            if (data == null)
            {
                FrameCount = 0;
                return;
            }

            if (!data.Uncompress())
                throw new Exception("Unsupported Compression, can't decompress.");

            if (!data.Elements.ContainsKey(DICOMTags.BitsAllocated))
                throw new MissingMemberException("Missing the Bits Allocated field, can't render.");

            bitsAllocated = (ushort)data.Elements[DICOMTags.BitsAllocated].Data;
            bytes = (bitsAllocated == 8) ? 1 : 2;

            if (!data.Elements.ContainsKey(DICOMTags.PhotometricInterpretation))
                throw new MissingMemberException("Missing the Photometric Interpretation field, can't render.");

            photoInterp = data.Elements[DICOMTags.PhotometricInterpretation].Display.Trim();
            bFlipMono = (photoInterp == "MONOCHROME1");
            bRGB = (photoInterp == "RGB");
            bPalette = (photoInterp == "PALETTE COLOR");

            if (bPalette)
            {
                if (!data.Elements.ContainsKey(DICOMTags.RedPaletteColorLookupTableData) || !data.Elements.ContainsKey(DICOMTags.GreenPaletteColorLookupTableData) || !data.Elements.ContainsKey(DICOMTags.BluePaletteColorLookupTableData))
                    throw new MissingMemberException("Missing a Palette Lookup field on a PALETTE COLORed image, can't render.");

                palettes = new byte[3][] {
                    (byte[])data.Elements[DICOMTags.RedPaletteColorLookupTableData].Data,
                    (byte[])data.Elements[DICOMTags.GreenPaletteColorLookupTableData].Data,
                    (byte[])data.Elements[DICOMTags.BluePaletteColorLookupTableData].Data };

                //PS 3, Page 417-418
                if (!data.Elements.ContainsKey(DICOMTags.RedPaletteColorLookupTableDescriptor) || !data.Elements.ContainsKey(DICOMTags.GreenPaletteColorLookupTableDescriptor) || !data.Elements.ContainsKey(DICOMTags.BluePaletteColorLookupTableDescriptor))
                    throw new MissingMemberException("Missing a Palette Descriptor field on a PALETTE COLORed image, can't render.");
                palette16 = (((ushort[])data.Elements[DICOMTags.RedPaletteColorLookupTableDescriptor].Data)[2] == 16);
                paletteNumEntries = ((ushort[])data.Elements[DICOMTags.RedPaletteColorLookupTableDescriptor].Data)[0];
                if (paletteNumEntries == 0)
                {
                    // Apparently 0 means 65536
                    paletteNumEntries = 65536;
                }
                paletteFirstEntry = ((ushort[])data.Elements[DICOMTags.RedPaletteColorLookupTableDescriptor].Data)[1];
                //figure out supplemental palette color LUT at some point

                if (palette16)
                {
                    // Flip endianness of the palettes
                    palettes16 = new ushort[3][]
                    {
                        new ushort[paletteNumEntries],
                        new ushort[paletteNumEntries],
                        new ushort[paletteNumEntries]
                    };

                    for (int i = 0; i < paletteNumEntries; i++)
                    {
                        palettes16[0][i] = (ushort)(palettes[0][2 * i + 1] | (palettes[0][2 * i] << 8));
                        palettes16[1][i] = (ushort)(palettes[1][2 * i + 1] | (palettes[1][2 * i] << 8));
                        palettes16[2][i] = (ushort)(palettes[2][2 * i + 1] | (palettes[2][2 * i] << 8));
                    }
                }
            }
            else
            {
                palettes = new byte[3][];
                palettes16 = new ushort[3][];
            }

            bPlanarOne = false;
            if (data.Elements.ContainsKey(DICOMTags.PlanarConfiguration))
                bPlanarOne = ((ushort)data.Elements[DICOMTags.PlanarConfiguration].Data == 1);

            if (!data.Elements.ContainsKey(DICOMTags.ImageWidth))
                throw new MissingMemberException("Missing the Image Width field, can't render.");
            if (!data.Elements.ContainsKey(DICOMTags.ImageHeight))
                throw new MissingMemberException("Missing the Image Height field, can't render.");

            imWidth = (ushort)data.Elements[DICOMTags.ImageWidth].Data;
            imHeight = (ushort)data.Elements[DICOMTags.ImageHeight].Data;

            signedData = false;
            if (data.Elements.ContainsKey(DICOMTags.PixelRepresentation))
            {
                signedData = Convert.ToInt32(data.Elements[DICOMTags.PixelRepresentation].Data) == 1;
            }

            bRescaling = (data.Elements.ContainsKey(DICOMTags.RescaleIntercept) && data.Elements.ContainsKey(DICOMTags.RescaleSlope));
            adjM = 1.0f; adjB = 0;
            if (bRescaling)
            {
                adjB = float.Parse((string)data.Elements[DICOMTags.RescaleIntercept].Data);
                adjM = float.Parse((string)data.Elements[DICOMTags.RescaleSlope].Data);
                if (adjB == 0 && adjM == 1.0f)
                    bRescaling = false;
            }

            if (data.Elements.ContainsKey(DICOMTags.PixelIntensityRelationship))
            {
                string intensityRelationship = data.Elements[DICOMTags.PixelIntensityRelationship].Data.ToString().Trim().ToUpperInvariant();
                if (intensityRelationship == "LIN")
                {
                    if (data.Elements.ContainsKey(DICOMTags.PixelIntensityRelationshipSign))
                    {
                        float newAdj = float.Parse(data.Elements[DICOMTags.PixelIntensityRelationshipSign].Data.ToString());
                        if (newAdj == -1)
                        {
                            adjM *= newAdj;

                            ushort bitsStored = (ushort)data.Elements[DICOMTags.BitsStored].Data;
                            if (bitsStored == 16)
                            {
                                adjB += 65536;
                            }
                            else if (bitsStored == 12)
                            {
                                adjB += 4096;
                            }
                            else if (bitsStored == 8)
                            {
                                adjB += 256;
                            }

                            bRescaling = true;
                        }
                    }
                }
            }

            bMultiframe = (data.Elements.ContainsKey(DICOMTags.NumberOfFrames));
            if (bMultiframe)
                FrameCount = int.Parse((string)data.Elements[DICOMTags.NumberOfFrames].Data);
            else
                FrameCount = 1;

            if (!data.Elements.ContainsKey(DICOMTags.PixelData))
                throw new MissingMemberException("Missing the Pixel Data field, can't render.");
            pixelData = data.Elements[DICOMTags.PixelData];
        }

        private byte[] GetFrameData(int frameNum, out int dataOffset, out int dataLength)
        {
            if (bMultiframe)
            {
                if (frameNum >= FrameCount)
                    throw new ArgumentException("Frame Number specified (" + frameNum + ", 0-based) too high. Image only contains " + FrameCount + " frames.");

                if (pixelData.VRShort == DICOMElementSQ.vrshort)
                {
                    //Sequence!
                    List<SQItem> sqItems = (List<SQItem>)pixelData.Data;
                    byte[] retData = sqItems[frameNum + 1].EncapsulatedImageData;
                    dataOffset = 0;
                    dataLength = retData.Length;
                    return retData;
                }
                else
                {
                    dataLength = imWidth * imHeight * bytes * (bRGB ? 3 : 1);
                    dataOffset = frameNum * dataLength;
                    return (byte[])pixelData.Data;
                }
            }
            else
            {
                if (frameNum > 0)
                    throw new ArgumentException("Frame Number specified (" + frameNum + ") too high. Image is not multiframe.");

                byte[] retData = (byte[])data.Elements[DICOMTags.PixelData].Data;
                dataOffset = 0;
                dataLength = retData.Length;
                return retData;
            }
        }

        /// <summary>
        /// Automatically calculate the window and level for the dataset.  If it is contained in the image as tags,
        /// they are used directly.  If not, then an appropriate window/level are calculated from the pixel data.
        /// Note: Calling this function does not automatically cause a redraw.
        /// </summary>
        /// <param name="frameNum">If this is a multiframe image, this determines which frame the window/level are
        /// calculated for.  If this is a single frame image, this parameter is ignored.</param>
        public void CalculateWindowLevel(int frameNum)
        {
            int dataOffset, dataLength;
            byte[] sourceData = GetFrameData(frameNum, out dataOffset, out dataLength);

            //figure out window/level
            if (data.Elements.ContainsKey(DICOMTags.WindowCenter) && data.Elements.ContainsKey(DICOMTags.WindowWidth))
            {
                //pull window/level out of file
                Window = (int) float.Parse(StringUtil.GetFirstFromPossibleNMultiplicity(data.Elements[DICOMTags.WindowWidth].Display));
                Level = (int) float.Parse(StringUtil.GetFirstFromPossibleNMultiplicity(data.Elements[DICOMTags.WindowCenter].Display));

                if ((bitsAllocated == 8) && ((Window > 256) || (Level > 256)))
                {
                    Window = 256;
                    Level = 128;
                }
            }
            else
            {
                //no window/level in file... use crappy made-up defaults or try to calculate...
                if (bPalette)
                {
                    //try to calculate from image data...
                    int min = int.MaxValue, max = int.MinValue;
                    unsafe
                    {
                        fixed (byte* byteData = sourceData)
                        {
                            if (bytes == 2)
                            {
                                ushort* shortData = (ushort*)byteData;

                                for (int i = dataOffset / 2; i < (dataOffset + dataLength) / 2; i++)
                                {
                                    ushort psrc = (ushort) (shortData[i] - paletteFirstEntry);
                                    if (psrc < 0) psrc = 0; else if (psrc >= paletteNumEntries) psrc = (ushort)(paletteNumEntries - 1);

                                    for (int b = 0; b < 3; b++)
                                    {
                                        int src = palette16 ? palettes16[b][psrc] : palettes[b][psrc];

                                        if (bRescaling) src = (short)(src * adjM + adjB);
                                        if (src < min) min = src;
                                        if (src > max) max = src;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = dataOffset; i < dataOffset + dataLength; i++)
                                {
                                    byte psrc = (byte)(byteData[i] - paletteFirstEntry);
                                    if (psrc < 0) psrc = 0; else if (psrc >= paletteNumEntries) psrc = (byte)(paletteNumEntries - 1);

                                    for (int b = 0; b < 3; b++)
                                    {
                                        int src = palette16 ? palettes16[b][psrc] : palettes[b][psrc];

                                        if (bRescaling) src = (short)(src * adjM + adjB);
                                        if (src < min) min = src;
                                        if (src > max) max = src;
                                    }
                                }
                            }
                        }
                    }

                    Window = (max - min);
                    Level = ((max + min) / 2);
                }
                else if (bRGB)
                {
                    Window = 256;
                    Level = 128;
                }
                else if (bitsAllocated == 8)
                {
                    //try to calculate from image data...
                    byte min = byte.MaxValue, max = byte.MinValue;
                    unsafe
                    {
                        fixed (byte* byteData = sourceData)
                        {
                            for (int i = dataOffset; i < dataOffset + dataLength; i++)
                            {
                                byte src = sourceData[i];
                                if (bRescaling) src = (byte)(src * adjM + adjB);
                                if (src < min) min = src;
                                if (src > max) max = src;
                            }
                        }
                    }
                    Window = (max - min);
                    Level = ((max + min) / 2);
                }
                else
                {
                    //try to calculate from image data...
                    int min = int.MaxValue, max = int.MinValue;
                    unsafe
                    {
                        fixed (byte* byteData = sourceData)
                        {
                            short* shortData = (short*)byteData;

                            for (int i = dataOffset / 2; i < (dataOffset + dataLength) / 2; i++)
                            {
                                int src = signedData ? (int) shortData[i] : (int) (ushort) shortData[i];
                                if (bRescaling) src = (short)(src * adjM + adjB);
                                if (src < min) min = src;
                                if (src > max) max = src;
                            }
                        }
                    }
                    Window = (max - min);
                    Level = ((max + min) / 2);
                }
            }
        }

        /// <summary>
        /// This function returns the current pixel pitch ratio for the display panel.  For example, if the image is 256x256 and the
        /// render size is currently 512x512, with a zoom of 1, then the image will be doubled to 512x512, so the pixel pitch is
        /// 0.5f, since each real rendered pixel is equivalent to 0.5 pixels of source image.
        /// </summary>
        /// <returns>The calculated pixel pitch.</returns>
        public float GetPixelPitch()
        {
            //find the lowest base zoom for the image to viewport ratio
            float HZoom = (float)size.Width / (float)imWidth;
            float VZoom = (float)size.Height / (float)imHeight;
            float baseZoom = (HZoom > VZoom) ? VZoom : HZoom;   //find the smallest

            //adjust to our settings
            return 1f / (baseZoom * Zoom);
        }

        /// <summary>
        /// Causes a full render of the frame with the current parameters.  The <see cref="RenderedImage"/> is updated with the new
        /// rendered frame.
        /// </summary>
        /// <param name="frameNum">If this is a multiframe image, this determines which frame is rendered.  If this is a single frame
        /// image, this parameter is ignored.</param>
        /// <param name="filter">True for bilinear filtering (slower but prettier), false for nearest neighbor filtering (faster but ugly).</param>
        public void RenderFrame(int frameNum, bool filter)
        {
            int dataOffset, dataLength;
            byte[] sourceData = GetFrameData(frameNum, out dataOffset, out dataLength);

            float pixelPitch = GetPixelPitch();

            float topleftX = -pixelPitch * (size.Width / 2f) - CenterX + imWidth / 2f;
            float topleftY = -pixelPitch * (size.Height / 2f) - CenterY + imHeight / 2f;

            if (filter)
            {
                topleftX -= 0.5f;
                topleftY -= 0.5f;
            }

            BitmapData bdata = RenderedImage.LockBits(new Rectangle(Point.Empty, RenderedImage.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            unsafe
            {
                fixed (byte* byteData = &sourceData[dataOffset])
                {
                    fixed (byte* p1 = palettes[0], p2 = palettes[1], p3 = palettes[2])
                    {
                        fixed (ushort* p16r = palettes16[0], p16g = palettes16[1], p16b = palettes16[2])
                        {
                            byte*[] pal8 = new byte*[3] { (byte*)p1, (byte*)p2, (byte*)p3 };
                            ushort*[] pal16 = new ushort*[3] { p16r, p16g, p16b };

                            short* shortData = (short*)byteData;

                            int frameOffset = imWidth * imHeight * bytes;

                            //BS vars to init for filtering later
                            float yf = 0, w1 = 0, w2 = 0, w3 = 0, w4 = 0;
                            ushort psrc10 = 0, psrc01 = 0, psrc11 = 0;
                            int pX1 = 0, pY1 = 0;

                            float yCoord = topleftY;
                            for (int y = 0; y < size.Height; y++, yCoord += pixelPitch)
                            {
                                byte* pBMPData = (byte*)bdata.Scan0 + y * bdata.Stride;

                                int pY = (int)yCoord;
                                if (filter)
                                {
                                    yf = yCoord - pY;
                                    pY1 = pY + 1;
                                    if (pY1 >= imHeight) pY1 = pY;
                                }

                                float xCoord = topleftX;
                                for (int x = 0; x < size.Width; x++, xCoord += pixelPitch)
                                {
                                    *((uint*)(pBMPData + 4 * x)) = 0xFF000000;

                                    int pX = (int)xCoord;

                                    if (pX < 0 || pX >= imWidth || pY < 0 || pY >= imHeight)
                                        continue;

                                    if (filter)
                                    {
                                        float xf = xCoord - pX;

                                        w1 = (1.0f - xf) * (1.0f - yf);
                                        w2 = (xf) * (1.0f - yf);
                                        w3 = (1.0f - xf) * (yf);
                                        w4 = (xf) * (yf);

                                        pX1 = pX + 1;
                                        if (pX1 >= imWidth) pX1 = pX;
                                    }

                                    //Check for buffer overruns using the worst case scenario
                                    if (filter)
                                    {
                                        //Lower right pixel for filter
                                        if (bytes * (pY1 * imWidth + pX1) >= dataLength)
                                            continue;
                                    }
                                    else
                                    {
                                        if (bytes * (pY * imWidth + pX) >= dataLength)
                                            continue;
                                    }

                                    if (bPalette)
                                    {
                                        //palette lookup. 1 byte -> 3 after lookup
                                        ushort psrc = (ushort) (((bytes == 2) ? (ushort)shortData[pY * imWidth + pX] : byteData[pY * imWidth + pX]) - paletteFirstEntry);
                                        if (psrc < 0) psrc = 0; else if (psrc >= paletteNumEntries) psrc = (ushort)(paletteNumEntries - 1);

                                        if (filter)
                                        {
                                            psrc10 = (ushort)(((bytes == 2) ? shortData[pY * imWidth + pX1] : byteData[pY * imWidth + pX1]) - paletteFirstEntry);
                                            if (psrc10 < 0) psrc10 = 0; else if (psrc10 >= paletteNumEntries) psrc10 = (ushort)(paletteNumEntries - 1);
                                            psrc01 = (ushort)(((bytes == 2) ? shortData[(pY1) * imWidth + pX] : byteData[(pY1) * imWidth + pX]) - paletteFirstEntry);
                                            if (psrc01 < 0) psrc01 = 0; else if (psrc01 >= paletteNumEntries) psrc01 = (ushort)(paletteNumEntries - 1);
                                            psrc11 = (ushort)(((bytes == 2) ? shortData[(pY1) * imWidth + pX1] : byteData[(pY1) * imWidth + pX1]) - paletteFirstEntry);
                                            if (psrc11 < 0) psrc11 = 0; else if (psrc11 >= paletteNumEntries) psrc11 = (ushort)(paletteNumEntries - 1);
                                        }

                                        for (int b = 0; b < 3; b++)
                                        {
                                            int src = palette16 ? pal16[b][psrc] : pal8[b][psrc];

                                            if (filter)
                                            {
                                                int src10 = palette16 ? pal16[b][psrc10] : pal8[b][psrc10];
                                                int src01 = palette16 ? pal16[b][psrc01] : pal8[b][psrc01];
                                                int src11 = palette16 ? pal16[b][psrc11] : pal8[b][psrc11];
                                                src = (int)((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4));
                                            }

                                            if (bRescaling) src = (int)(src * adjM + adjB);
                                            src = (int)((src - Level) * (255f / Window) + 128);
                                            if (src < 0) src = 0; else if (src > 255) src = 255;
                                            if (bFlipMono) src = (255 - src);
                                            pBMPData[4 * x + (2 - b)] = (byte)src;
                                        }
                                    }
                                    else if (bRGB)
                                    {
                                        if (bPlanarOne)
                                        {
                                            //RRR GGG BBB
                                            for (int b = 0; b < 3; b++)
                                            {
                                                int src = byteData[frameOffset * b + pY * imWidth + pX];

                                                if (filter)
                                                {
                                                    int src10 = byteData[frameOffset * b + pY * imWidth + pX1];
                                                    int src01 = byteData[frameOffset * b + pY1 * imWidth + pX];
                                                    int src11 = byteData[frameOffset * b + pY1 * imWidth + pX1];
                                                    src = (int)((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4));
                                                }

                                                if (bRescaling) src = (int)(src * adjM + adjB);
                                                src = (int)((src - Level) * (255f / Window) + 128);
                                                if (src < 0) src = 0; else if (src > 255) src = 255;
                                                if (bFlipMono) src = (255 - src);
                                                pBMPData[4 * x + (2 - b)] = (byte)src;
                                            }
                                        }
                                        else
                                        {
                                            //RGB RGB

                                            for (int b = 0; b < 3; b++)
                                            {
                                                int src = byteData[3 * (pY * imWidth + pX) + b];

                                                if (filter)
                                                {
                                                    int src10 = byteData[3 * (pY * imWidth + pX1) + b];
                                                    int src01 = byteData[3 * (pY1 * imWidth + pX) + b];
                                                    int src11 = byteData[3 * (pY1 * imWidth + pX1) + b];
                                                    src = (int)((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4));
                                                }

                                                if (bRescaling) src = (int)(src * adjM + adjB);
                                                src = (int)((src - Level) * (255f / Window) + 128);
                                                if (src < 0) src = 0; else if (src > 255) src = 255;
                                                if (bFlipMono) src = (255 - src);
                                                pBMPData[4 * x + (2 - b)] = (byte)src;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //grayscale
                                        int src = (bytes == 2) ? (signedData ? shortData[pY * imWidth + pX] : (int) (ushort) shortData[pY * imWidth + pX]) : byteData[pY * imWidth + pX];

                                        if (filter)
                                        {
                                            int src10 = (bytes == 2) ? (signedData ? shortData[pY * imWidth + pX1] : (int)(ushort)shortData[pY * imWidth + pX1]) : byteData[pY * imWidth + pX1];
                                            int src01 = (bytes == 2) ? (signedData ? shortData[(pY1) * imWidth + pX] : (int)(ushort)shortData[(pY1) * imWidth + pX]) : byteData[(pY1) * imWidth + pX];
                                            int src11 = (bytes == 2) ? (signedData ? shortData[(pY1) * imWidth + pX1] : (int)(ushort)shortData[(pY1) * imWidth + pX1]) : byteData[(pY1) * imWidth + pX1];
                                            src = (int)((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4));
                                        }

                                        if (bRescaling) src = (int)(src * adjM + adjB);
                                        src = (int)((src - Level) * (255f / Window) + 128);
                                        if (src < 0) src = 0; else if (src > 255) src = 255;
                                        if (bFlipMono) src = (255 - src);

                                        pBMPData[4 * x] = (byte)src;
                                        pBMPData[4 * x + 1] = (byte)src;
                                        pBMPData[4 * x + 2] = (byte)src;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            RenderedImage.UnlockBits(bdata);
        }

        /// <summary>
        /// Provides access to the image last rendered by the RenderAssistant.
        /// </summary>
        public Bitmap RenderedImage { get; private set; }

        /// <summary>
        /// The window (range of values visible) for the render.  This can be calculated automatically by <see cref="CalculateWindowLevel"/>
        /// or set manually.
        /// </summary>
        public int Window;
        /// <summary>
        /// The level (middle value visible) for the render.  This can be calculated automatically by <see cref="CalculateWindowLevel"/>
        /// or set manually.
        /// </summary>
        public int Level;

        /// <summary>
        /// The zoom value for the window.  This is not the ratio of source image size to how it is rendered in the window, it is actually
        /// a true ratio of how the image is rendered in the window.  If this is 1.0, then it is rendered so the image exactly fills the
        /// rendering.  If it is 2.0, then the image is exactly twice as big as can fit in the window.
        /// </summary>
        public float Zoom;
        /// <summary>
        /// The number of pixels to horizontally offset the center coordinate of the image when rendered.  If you are trying to match this to true
        /// pixels, then you need to take the pixel pitch into account with <see cref="GetPixelPitch"/>.
        /// </summary>
        public float CenterX;
        /// <summary>
        /// The number of pixels to vertically offset the center coordinate of the image when rendered.  If you are trying to match this to true
        /// pixels, then you need to take the pixel pitch into account with <see cref="GetPixelPitch"/>.
        /// </summary>
        public float CenterY;

        /// <summary>
        /// Returns the number of frames contained in the currently set DICOMData.
        /// </summary>
        public int FrameCount { get; private set; }


        //Private info about the stored image
        private DICOMData data;
        private Size size;

        private bool signedData;
        private int bitsAllocated, bytes;
        private bool bFlipMono, bRGB, bPalette;

        private bool palette16;
        private int paletteNumEntries;
        private ushort paletteFirstEntry;
        private byte[][] palettes;
        private ushort[][] palettes16;

        private string photoInterp;
        private bool bPlanarOne;
        private bool bMultiframe;
        private int imWidth, imHeight;

        private bool bRescaling;
        private float adjM = 1.0f, adjB = 0;
        private DICOMElement pixelData;
    }
}
