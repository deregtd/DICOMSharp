using System;
using DICOMSharp.Data;
using System.IO;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data.Compression;
using NUnit.Framework;
using System.Drawing;
using DICOMSharpControls.Imaging;
using DICOMSharp.Logging;

namespace DICOMSharpControls.Tests
{
    [TestFixture]
    internal class CompressionTest
    {
        [Test(Description="Tests full round trip (decompression and recompression) of the compression samples, and also attempts to render the output.")]
        public static void RoundTripAndRender()
        {
            foreach (FileInfo fi in new DirectoryInfo("..\\CompressionSamples").GetFiles("*.dcm"))
            {
                Console.WriteLine("Compression Test -- Filename: " + fi.Name);
                
                DICOMData data = new DICOMData();
                data.ParseFile(fi.FullName, true, new NullLogger());
                TransferSyntax syntax = data.TransferSyntax;
                
                int compressedLength = (int) data[DICOMTags.PixelData].GetDataLength(false);
                
                data.Uncompress();

                Image testImage = DICOMQuickRenderer.QuickRender(data, 0);
                Assert.IsNotNull(testImage);

                int uncompressedLength = (int)data[DICOMTags.PixelData].GetDataLength(false);
                Assert.IsTrue(uncompressedLength > compressedLength);

                if (CompressionWorker.SupportsCompression(syntax.Compression))
                {
                    data.ChangeTransferSyntax(syntax);
                    int recompressedLength = (int)data[DICOMTags.PixelData].GetDataLength(false);
                    Assert.IsTrue(recompressedLength < uncompressedLength);
                }

                //data.WriteFile(fi.FullName.Replace(".dcm", "_un.dcm"));
            }
        }
    }
}
