using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Logging;
using DICOMSharp.Network.Workers;
using DICOMSharp.Network.Connections;
using DICOMSharp.Data;

namespace DICOMSharpTests
{
    class ListenerTest
    {
        public static void Test()
        {
            DICOMListener listener = new DICOMListener(new ConsoleLogger(), true);
            listener.StoreRequest +=
                new DICOMListener.StoreRequestHandler(listener_StoreRequest);
            listener.StartListening(4007);

            Console.ReadLine();
            listener.StopListening();
        }

        static void listener_StoreRequest(DICOMConnection conn, DICOMData data)
        {
            //Do something with the stored image
        }
    }
}
