using System.Collections.Generic;
using DICOMSharp.Logging;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.Presentations;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;
using DICOMSharp.Network.QueryRetrieve;

namespace DICOMSharp.Network.Workers
{
    /// <summary>
    /// This class is a helper class representing a DICOM SCU that will attempt to perform a DICOM C-MOVE-RQ on an SCP,
    /// and provides updates back to the user via a callback about the progress of the move request.
    /// </summary>
    public class DICOMMover : DICOMSCU
    {
        /// <summary>
        /// Initialize a new mover class.  You must provide a logging mechanism and a verbosity level.
        /// </summary>
        /// <param name="logger">A logger to use for the connection.  May not be null.</param>
        /// <param name="moverName">String to identify the mover for logging</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        public DICOMMover(ILogger logger, string moverName, bool verbose) : base(logger, moverName, verbose) { }

        /// <summary>
        /// After setting up the mover with your callback(s), call this to start the move process.  All updates
        /// about the operation will be provided via the callbacks.
        /// </summary>
        /// <param name="hostAE">An entity containing the AE title to represent the SCU.</param>
        /// <param name="remoteAE">An entity containing the SCP to attempt to contact.</param>
        /// <param name="sendToAE">This is the AE Title of the move destination.  The remote AE must have full information
        /// about this entity (hostname/address and port), because only the title is passed via this request.</param>
        /// <param name="requestData">A filled out request for what images to target with the MOVE request.</param>
        public void StartMove(ApplicationEntity hostAE, ApplicationEntity remoteAE, string sendToAE, QRRequestData requestData)
        {
            this.FindRequest = requestData;
            this.SendToAE = sendToAE;

            if (requestData.QueryLevel == QueryRetrieveLevel.PatientRoot)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.PatientRootQueryRetrieveInformationModelMOVE, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));
            else if (requestData.QueryLevel == QueryRetrieveLevel.PatientStudyOnly)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.PatientStudyOnlyQueryRetrieveInformationModelMOVERetired, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));
            else if (requestData.QueryLevel == QueryRetrieveLevel.StudyRoot)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.StudyRootQueryRetrieveInformationModelMOVE, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));

            StartConnection(hostAE, remoteAE);
        }

        /// <summary>
        /// Start the move request
        /// </summary>
        protected override void conn_AssociationAccepted(DICOMConnection conn)
        {
            //fire off the request...
            conn.SendCMOVERQ(CommandPriority.Medium, FindRequest.QueryLevel, SendToAE, FindRequest.CreateSearchData());
        }

        /// <summary>
        /// Parse commands
        /// </summary>
        protected override void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM)
        {
            switch (command)
            {
                case PDATACommands.CMOVERSP:
                    logger.Log(LogLevel.Info, "Received C-MOVE-RSP");

                    CommandStatus status = (CommandStatus)(ushort)cmdDICOM[DICOMTags.Status].Data;
                    ushort remaining = (ushort)cmdDICOM[DICOMTags.NumberOfRemainingSubOps].Data;
                    ushort completed = (ushort)cmdDICOM[DICOMTags.NumberOfCompletedSubOps].Data;
                    ushort failed = (ushort)cmdDICOM[DICOMTags.NumberOfFailedSubOps].Data;
                    ushort warning = (ushort)cmdDICOM[DICOMTags.NumberOfWarningSubOps].Data;

                    if (MoveUpdate != null)
                        MoveUpdate(this, remaining, completed, failed, warning);

                    if (remaining == 0)
                        conn.SendReleaseRQ();

                    break;
                default:
                    logger.Log(LogLevel.Warning, "Unhandled P-DATA Command Type: " + command + "...");
                    break;
            }
        }

        /// <summary>
        /// If you aren't keeping track of the called/target AE for this operation, this stores it for you after the StartMove call is made.
        /// </summary>
        public string TargetAE { get { if (conn != null) return conn.CalledAE; else return null; } }

        /// <summary>
        /// If you aren't keeping track of the move destination (who the SCP will send the DICOM to), this stores it for you after the
        /// StartMove call is made.
        /// </summary>
        public string SendToAE { get; private set; }

        /// <summary>
        /// This is a handler for the <see cref="MoveUpdate"/> callback.
        /// </summary>
        /// <param name="mover">A pointer to the DICOMMover that is sending this update, in case you are using the same callback function
        /// for many simultaneous movers.</param>
        /// <param name="remaining">The number of instances left to send.</param>
        /// <param name="completed">The number of instances successfully sent.</param>
        /// <param name="failed">The number of instances that failed to send.</param>
        /// <param name="warning">The number of instances that were sent with warnings.</param>
        public delegate void MoveUpdateHandler(DICOMMover mover, ushort remaining, ushort completed, ushort failed, ushort warning);
        /// <summary>
        /// Assign a callback function to this handler to get updates about the move request.
        /// It will get called whenver we get a C-MOVE-RSP from the SCP.
        /// </summary>
        public event MoveUpdateHandler MoveUpdate;


        private QRRequestData FindRequest;
    }
}
