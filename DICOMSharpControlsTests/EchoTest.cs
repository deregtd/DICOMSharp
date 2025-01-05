using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Logging;
using DICOMSharp.Network.Workers;
using DICOMSharp.Network.Connections;

namespace DICOMSharpTests
{
    class EchoTest
    {
        public static void Test()
        {
            DICOMEchoer echo = new DICOMEchoer(new NullLogger(), "", false);
            echo.Echo(
                new ApplicationEntity("ECHOTEST"),
                new ApplicationEntity("EFILM", "127.0.0.1", 4006)
                );
        }
    }
}
