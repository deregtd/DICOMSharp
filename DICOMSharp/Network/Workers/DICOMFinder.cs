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
    /// This class is a helper class representing a DICOM SCU that will attempt to perform a DICOM QUERY/FIND on an SCP.
    /// </summary>
    public class DICOMFinder : DICOMSCU
    {
        /// <summary>
        /// Initialize a new sender.  You must provide a logging mechanism and a verbosity level.
        /// </summary>
        /// <param name="logger">A logger to get logging callbacks about the exchange.  May not be null.</param>
        /// <param name="finderName">A string to identify the finder for logging</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        public DICOMFinder(ILogger logger, string finderName, bool verbose) : base(logger, finderName, verbose) { }

        /// <summary>
        /// Starts a C-FIND-RQ operation on a target entity.  The request will be made, and all responses will be aggregated into
        /// a single response structure, which will be returned via the <see cref="FindResponse"/> callback.
        /// </summary>
        /// <param name="hostAE">An entity containing the AE title to represent the SCU.</param>
        /// <param name="remoteAE">An entity containing the SCP to attempt to contact.</param>
        /// <param name="requestData">A filled out <see cref="QRRequestData"/> class with the request information.</param>
        public void Find(ApplicationEntity hostAE, ApplicationEntity remoteAE, QRRequestData requestData)
        {
            findResponse = requestData.GenerateResponse();

            FindRequest = requestData;

            if (requestData.QueryLevel == QueryRetrieveLevel.PatientRoot)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.PatientRootQueryRetrieveInformationModelFIND, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));
            else if (requestData.QueryLevel == QueryRetrieveLevel.PatientStudyOnly)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.PatientStudyOnlyQueryRetrieveInformationModelFINDRetired, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));
            else if (requestData.QueryLevel == QueryRetrieveLevel.StudyRoot)
                conn.AddPresentationContext(new PresentationContext(AbstractSyntaxes.StudyRootQueryRetrieveInformationModelFIND, new List<TransferSyntax> { TransferSyntaxes.ImplicitVRLittleEndian }));

            StartConnection(hostAE, remoteAE);
        }

        /// <summary>
        /// Start the find request
        /// </summary>
        protected override void conn_AssociationAccepted(DICOMConnection conn)
        {
            //fire off the request...
            conn.SendCFINDRQ(CommandPriority.Medium, FindRequest.QueryLevel, FindRequest.CreateSearchData());
        }

        /// <summary>
        /// Parse commands
        /// </summary>
        protected override void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM)
        {
            switch (command)
            {
                case PDATACommands.CFINDRSP:
                    logger.Log(LogLevel.Info, "Received C-FIND-RSP");

                    CommandStatus status = (CommandStatus)(ushort)cmdDICOM[DICOMTags.Status].Data;
                    if (status == CommandStatus.Pending_AllOptionalKeysReturned || status == CommandStatus.Pending_SomeOptionalKeysNotReturned)
                        findResponse.AddResponseRow(dataDICOM);
                    else if (status == CommandStatus.Success)
                    {
                        if (FindResponse != null)
                            FindResponse(this, findResponse);

                        conn.SendReleaseRQ();
                    }
                    else
                        logger.Log(LogLevel.Error, "Unhandled C-FIND-RSP Status Type: " + status + "...");

                    break;
                default:
                    logger.Log(LogLevel.Warning, "Unhandled P-DATA Command Type: " + command + "...");
                    break;
            }
        }

        /// <summary>
        /// If you haven't stored the target AE for this finder, this will provide it for you.
        /// </summary>
        public string TargetAE { get { if (conn != null) return conn.CalledAE; else return null; } }

        /// <summary>
        /// Contains the request that the find was initiated with in case you need to reference it later.
        /// </summary>
        public QRRequestData FindRequest { get; private set; }

        /// <summary>
        /// A callback handler for <see cref="FindResponse"/>.
        /// </summary>
        /// <param name="finder">A reference to the DICOMFinder that initiated the query that produced this response.</param>
        /// <param name="response">A parsed response structure with all of the responses to the find request.</param>
        public delegate void FindResponseHandler(DICOMFinder finder, QRResponseData response);
        /// <summary>
        /// This callback will be called when the find request is complete, and all responses are gathered.  Then this will
        /// be called with the aggregated response structure.  If an error occurs at any point, this callback will never
        /// be called.
        /// </summary>
        public event FindResponseHandler FindResponse;


        private QRResponseData findResponse;
    }
}
