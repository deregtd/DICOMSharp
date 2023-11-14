using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Network.Workers;
using DICOMSharp.Logging;
using System.IO;
using DICOMSharp.Data;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data.Tags;
using DICOMSharp.Util;
using System.Drawing;
using System.Drawing.Imaging;
using DICOMSharpControls.Imaging;

namespace DICOMSharpTests
{
    class Program
    {
        static SubscribableListener logger = new SubscribableListener();

        static void Main(string[] args)
        {
            logger.MessageLogged += new SubscribableListener.LoggedMessageHandler(logger_MessageLogged);

            //EchoTest.Test(logger, false);
            //ListenerTest.Test(logger);
            
            //DICOMSharp.Tests.CompressionTest.RoundTripAndRender();
            //TestThumbnails();
        }

        static void TestThumbnails()
        {
            foreach (FileInfo fi in new DirectoryInfo("..\\CompressionSamples").GetFiles("*.dcm"))
            //foreach (FileInfo fi in new DirectoryInfo("..\\CompressionSamples").GetFiles("JPEG-LS Lossless.dcm"))
            {
                Console.WriteLine("Compression Test -- Filename: " + fi.Name);

                DICOMData data = new DICOMData();
                data.ParseFile(fi.FullName, true, new NullLogger());
                //data.Uncompress();
                //data.WriteFile(fi.FullName.Replace(".dcm", "_un.dcm"));

                Image testImage = DICOMQuickRenderer.QuickRender(data, 0);

                testImage.Save(fi.FullName.Replace(".dcm", ".png"), ImageFormat.Png);
            }

        }

        static void logger_MessageLogged(LogLevel level, string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + level + ": " + message);
        }
    }
}
