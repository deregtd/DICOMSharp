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

namespace DICOMSharpTests
{
    class Program
    {
        static SubscribableListener logger = new SubscribableListener();

        static void Main(string[] args)
        {
            logger.MessageLogged += new SubscribableListener.LoggedMessageHandler(logger_MessageLogged);

            EchoTest.Test(logger, false);
            //ListenerTest.Test(logger);
            
            //DICOMSharp.Tests.CompressionTest.RoundTripAndRender();
            //TestThumbnails();
        }

        static void logger_MessageLogged(LogLevel level, string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + level + ": " + message);
        }
    }
}
