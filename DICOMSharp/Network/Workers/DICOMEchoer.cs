using System.Collections.Generic;
using DICOMSharp.Logging;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.Presentations;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data;

namespace DICOMSharp.Network.Workers
{
    /// <summary>
    /// This class is a helper class representing a DICOM SCU that will attempt to send a C-ECHO request to a target.
    /// </summary>
    public sealed class DICOMEchoer : DICOMSCU
    {
        /// <summary>
        /// Initialize a new echoer.  You must provide a logging mechanism and a verbosity level.
        /// </summary>
        /// <param name="logger">A logger to get logging callbacks about the exchange.  May not be null.</param>
        /// <param name="echoerName">A name to identify the echoer for logging</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        public DICOMEchoer(ILogger logger, string echoerName, bool verbose) : base(logger, echoerName, verbose)
        {
            conn.AddPresentationContext(new PresentationContext(
                AbstractSyntaxes.VerificationSOPClass,
                new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }
                ));
        }

        /// <summary>
        /// Tell the echoer to begin an attempt to echo a target.  Make sure to hook the <see cref="DICOMSCU.SCUFinished"/>
        /// callback to hear whether or not the echo worked.
        /// </summary>
        /// <param name="hostAE">An entity containing the AE title to represent the SCU.</param>
        /// <param name="remoteAE">An entity containing the SCP to attempt to contact.</param>
        public void Echo(ApplicationEntity hostAE, ApplicationEntity remoteAE)
        {
            StartConnection(hostAE, remoteAE);
        }

        /// <summary>
        /// Sends the echo request
        /// </summary>
        protected override void conn_AssociationAccepted(DICOMConnection conn)
        {
            conn.SendCECHORQ();
        }

        /// <summary>
        /// Parses commands.
        /// </summary>
        protected override void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM)
        {
            switch (command)
            {
                case PDATACommands.CECHORSP:
                    logger.Log(LogLevel.Info, "Received C-ECHO-RSP");
                    conn.SendReleaseRQ();
                    break;
                default:
                    logger.Log(LogLevel.Warning, "Unhandled P-DATA Command Type: " + command + "...");
                    break;
            }
        }
    }
}
