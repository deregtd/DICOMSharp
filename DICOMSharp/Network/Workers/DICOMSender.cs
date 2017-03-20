using System.Collections.Generic;
using DICOMSharp.Logging;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.Presentations;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;
using System.IO;
using System;
using DICOMSharp.Data.Elements;
using DICOMSharp.Network.QueryRetrieve;

namespace DICOMSharp.Network.Workers
{
    /// <summary>
    /// This class is a helper class representing a DICOM SCU that will attempt to send a set of images to a target SCP.
    /// </summary>
    public class DICOMSender : DICOMSCU
    {
        /// <summary>
        /// Initialize a new sender.  You must provide a logging mechanism and a verbosity level.
        /// </summary>
        /// <param name="logger">A DICOM logger for logging calls.  May not be null.</param>
        /// <param name="senderName">A string to identify the sender for logging</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        public DICOMSender(ILogger logger, string senderName, bool verbose) : base(logger, senderName, verbose)
        {
            sendQueue = new Queue<SendableImage>();

            moveInitiatorAETitle = null;
            completed = warned = failed = 0;
        }

        /// <summary>
        /// Attempt to pull an abstract syntax out of a DICOMData.
        /// </summary>
        /// <param name="data">The DICOMData to extract from.</param>
        /// <returns>The abstract syntax from either the media storage Sop class uid field or the sop class uid field.
        /// If both are missing, returns CT Image Storage.</returns>
        public static AbstractSyntax ExtractAbstractSyntaxFromDicomData(DICOMData data)
        {
            var MediaStorageSOPClassUID = data.Elements[DICOMTags.MediaStorageSOPClassUID] as DICOMElementUI;
            if (MediaStorageSOPClassUID != null)
            {
                return AbstractSyntaxes.Lookup(MediaStorageSOPClassUID.Data as string);
            }
            else
            {
                var SOPClassUID = data.Elements[DICOMTags.SOPClassUID] as DICOMElementUI;
                if (SOPClassUID != null)
                {
                    return AbstractSyntaxes.Lookup(SOPClassUID.Data as string);
                }
                else
                {
                    // Fall back to basic CT image storage...
                    return AbstractSyntaxes.CTImageStorage;
                }
            }
        }

        /// <summary>
        /// Initiates the sending process with the remote entity.  If you are using DICOMSender to respond to a
        /// C-MOVE-RQ, make sure you fill out the <see cref="moveInitiatorAETitle"/> and <see cref="moveMessageID"/>.
        /// </summary>
        /// <param name="hostAE">An entity containing the AE title to represent the SCU.</param>
        /// <param name="remoteAE">An entity containing the SCP to attempt to contact.</param>
        /// <param name="sendingObjects">An enumerable collection of <see cref="SendableImage" /> objects to send.</param>
        /// <param name="moveInitiatorAETitle">If this is being used in response to a C-MOVE-RQ, this is the AE title of the move initiator.  Otherwise null.</param>
        /// <param name="moveMessageID">If this is being used in response to a C-MOVE-RQ, this is the Message ID of the initial C-MOVE-RQ.  Otherwise ignored.</param>
        public void Send(ApplicationEntity hostAE, ApplicationEntity remoteAE, IEnumerable<SendableImage> sendingObjects, string moveInitiatorAETitle = null, ushort moveMessageID = 0)
        {
            foreach (var obj in sendingObjects)
            {
                sendQueue.Enqueue(obj);

                var transSyntax = obj.TransferSyntax ?? obj.DicomData.TransferSyntax;
                var abstractSyntax = obj.AbstractSyntax ?? ExtractAbstractSyntaxFromDicomData(obj.DicomData);

                conn.EnsureAbstractAndTransferSyntaxesHandled(abstractSyntax, transSyntax);
            }

            conn.EnsureUsableTransferSyntaxesExist();

            this.moveInitiatorAETitle = moveInitiatorAETitle;
            this.moveMessageID = moveMessageID;

            StartConnection(hostAE, remoteAE);
        }

        /// <summary>
        /// Start sending images
        /// </summary>
        /// <param name="conn"></param>
        protected override void conn_AssociationAccepted(DICOMConnection conn)
        {
            SendNextImage();
        }

        private void SendNextImage()
        {
            DICOMData data = null;
            while (sendQueue.Count > 0)
            {
                var nextImg = sendQueue.Dequeue();

                try
                {
                    data = nextImg.DicomData;

                    if (data == null)
                    {
                        if (!File.Exists(nextImg.FilePath))
                        {
                            logger.Log(LogLevel.Error, "File doesn't exist: " + nextImg.FilePath);

                            failed++;
                            if (SendUpdate != null)
                            {
                                SendUpdate(this, (ushort)sendQueue.Count, completed, warned, failed);
                            }
                            continue;
                        }

                        data = new DICOMData();
                        if (!data.ParseFile(nextImg.FilePath, true, logger))
                        {
                            failed++;
                            if (SendUpdate != null)
                            {
                                SendUpdate(this, (ushort)sendQueue.Count, completed, warned, failed);
                            }
                            continue;
                        }
                    }

                    // See if anyone wants to do any work to it
                    if (PreSendImage != null)
                    {
                        PreSendImage(this, data);
                    }

                    AbstractSyntax absSyntax = AbstractSyntaxes.CTImageStorage;
                    if (data.Elements.ContainsKey(DICOMTags.SOPClassUID))
                        absSyntax = AbstractSyntaxes.Lookup((string)data[DICOMTags.SOPClassUID].Data);
                    conn.SendCSTORERQ(data, absSyntax, CommandPriority.Medium, moveInitiatorAETitle, moveMessageID);
                }
                catch (Exception e)
                {
                    logger.Log(LogLevel.Error, "Unknown error encountered in DICOMSender.SendNextImage: " + e.Message);
                    failed++;
                    if (SendUpdate != null)
                        SendUpdate(this, (ushort)sendQueue.Count, completed, warned, failed);
                }

                return;
            }

            conn.SendReleaseRQ();
        }

        /// <summary>
        /// Process commands
        /// </summary>
        protected override void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM)
        {
            switch (command)
            {
                case PDATACommands.CSTORERSP:
                    logger.Log(LogLevel.Info, "Received C-STORE-RSP");

                    CommandStatus status = (CommandStatus)(ushort)cmdDICOM[DICOMTags.Status].Data;
                    if (status == CommandStatus.Success)
                        completed++;
                    else if (status == CommandStatus.Warning_DuplicateInvocation || status == CommandStatus.Warning_DuplicateSOPInstance)
                        warned++;
                    else
                        failed++;

                    if (SendUpdate != null)
                        SendUpdate(this, (ushort)sendQueue.Count, completed, warned, failed);

                    SendNextImage();

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
        /// A handler for the <see cref="SendUpdate"/> callback.
        /// </summary>
        /// <param name="sender">Contains a reference to the DICOMSender that's making this callback.</param>
        /// <param name="remaining">The number of images remaining to send.</param>
        /// <param name="completed">The number of images successfully sent.</param>
        /// <param name="warned">The number of images sent with warnings.</param>
        /// <param name="failed">The number of images that failed to send.</param>
        public delegate void SendUpdateHandler(DICOMSender sender, ushort remaining, ushort completed, ushort warned, ushort failed);
        /// <summary>
        /// This callback is called every time we get a C-STORE-RSP from the SCP with information about the send.
        /// </summary>
        public event SendUpdateHandler SendUpdate;

        /// <summary>
        /// A handler for the <see cref="PreSendImage"/> callback.
        /// </summary>
        /// <param name="sender">Contains a reference to the DICOMSender that's making this callback.</param>
        /// <param name="imageAboutToSend">A reference to the image file that is about to be sent to the target AE</param>
        public delegate void PreSendImageHandler(DICOMSender sender, DICOMData imageAboutToSend);
        /// <summary>
        /// This callback is called immediately prior to sending each image instance.  If you want to do
        /// work on an image such as anonymize or decompress it, here is the way to do it.
        /// </summary>
        public event PreSendImageHandler PreSendImage;

        private Queue<SendableImage> sendQueue;
        private ushort completed, warned, failed;

        private ushort moveMessageID;
        private string moveInitiatorAETitle;
    }
}
