using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using DICOMSharp.Data;
using System.IO;
using DICOMSharp.Logging;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Util;
using DICOMSharp.Data.Tags;
using DICOMSharp.Network.Presentations;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Network.QueryRetrieve;
using System.ComponentModel;
using System.Text;
using DICOMSharp.Network.Workers;
using System.Net;
using DICOMSharp.Data.Compression;

namespace DICOMSharp.Network.Connections
{
    /// <summary>
    /// DICOMConnection is a master class for handling a DICOM network connection.  It has callbacks for all common event handling.
    /// You can make new DICOM helpers by either subclassing or encapsulating DICOMConnection, or just using individual DICOMConnections separately.
    /// </summary>
    public class DICOMConnection
    {
        /// <summary>
        /// Create a new DICOMConnection.
        /// </summary>
        /// <param name="logger">A logger of some sort to receive debugging.</param>
        /// <param name="connectionName">String to identify the connection</param>
        /// <param name="verboseLogging">True means high verbosity logging, false means normal logging.</param>
        public DICOMConnection(ILogger logger, string connectionName, bool verboseLogging)
        {
            this._connectionName = connectionName;
            this.logger = new PrefixingLogger(logger, "[" + this._connectionName + "] ");
            this.verboseLogging = verboseLogging;

            IsConnected = false;
            MyMaxPDU = 0xC80E;
            PresentationContexts = new Dictionary<byte, PresentationContext>();
            presentationContextLookupBySyntax = new Dictionary<AbstractSyntax, PresentationContext>();
            Associated = false;
            Released = false;
            nextMessageID = 1;
            nextPresentationContextID = 1;
            SupportedAbstractSyntaxes = new HashSet<AbstractSyntax>();

            commandStream = new MemoryStream();
            dataStream = new MemoryStream();

            getHandling = false;
            getMessageID = 0;
            getResponse = null;

            moveSender = null;

            monitorThread = new Thread(new ThreadStart(MonitorConn));
        }

        /// <summary>
        /// Attempt to connect to a target machine for DICOM communication.  Make sure to fill in presentation contexts to use before calling this function, as it
        /// starts the association process (sends an Associate-RQ) on a successful connection.
        /// </summary>
        /// <param name="entity">Remote entity to connect to (this auto-sets CalledAE as well)</param>
        /// <returns>Successful connections return true, otherwise false with a logged error.</returns>
        public bool ConnectTo(ApplicationEntity entity)
        {
            CalledAE = entity.Title;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sock.Connect(entity.Address, entity.Port);

                HandleSocket(sock);

                SendAssociateRQ();

                return true;
            }
            catch (SocketException e)
            {
                logger.Log(LogLevel.Error, "ConnectTo Error: " + e);
                return false;
            }
        }


        /// <summary>
        /// Tells the DICOMConnection to start handling DICOM communication on a socket.  This is useful if you receive a TCP connection that you know is DICOM
        /// and want DICOMConnection to deal with it.
        /// </summary>
        /// <param name="remoteSocket">The socket to handle communication on</param>
        public void HandleSocket(Socket remoteSocket)
        {
            this.RemoteSocket = remoteSocket;
            LastAction = DateTime.Now;

            monitorThread.Start();
        }

        private void SendData(byte[] data)
        {
            try
            {
                RemoteSocket.Send(data);
            }
            catch
            {
                //hm.  something nasty happened.  close out!
                CloseConnection();
            }

            LastAction = DateTime.Now;
        }

        /// <summary>
        /// Closes the connection, if it is open.  Also sends a Release-RQ if the connection is associated but not yet released.
        /// </summary>
        public void CloseConnection()
        {
            if (Associated && !Released)
                SendReleaseRQ();

            if (moveSender != null)
                moveSender.Abort();

            RemoteSocket.Close();
            //RemoteSocket = null;

            if (ConnectionClosed != null)
                ConnectionClosed(this);
        }

        private void MonitorConn()
        {
            try
            {
                IsConnected = true;
                byte[] readQueue = new byte[MyMaxPDU];
                MemoryStream readStream = null;
                int readStreamPacketLen = 0;

                while (RemoteSocket.Connected)
                {
                    int readQueueLen = RemoteSocket.Receive(readQueue, 0, (int)(MyMaxPDU), SocketFlags.None);
                    if (readQueueLen == 0 && RemoteSocket.Blocking)
                    {
                        logger.Log(LogLevel.Warning, "Remote Socket Disappeared...");
                        RemoteSocket.Close();
                        continue;
                    }

                    int readPtr = 0, readQueueLeft = readQueueLen;

                    LastAction = DateTime.Now;

                    bool needLoop = false;
                    do
                    {
                        needLoop = false;
                        if (readStream != null)
                        {
                            //Middle of a longer packet...
                            if (readStream.Length + readQueueLeft < 6)
                            {
                                //Somehow still not long enough to make a full packet...  Write it and keep trying, I guess.
                                readStream.Write(readQueue, readPtr, readQueueLeft);
                            }
                            else
                            {
                                //Enough to make a packet header at least

                                if (readStreamPacketLen == 0)
                                {
                                    //Don't know packet length yet...  So find it.  Fill in just enough to get the length for now.
                                    int bytesPopped = 6 - (int)readStream.Length;
                                    readStream.Write(readQueue, readPtr, bytesPopped);

                                    SwappableBinaryReader pduReader = new SwappableBinaryReader(readStream, true);
                                    readStream.Seek(2, SeekOrigin.Begin);
                                    readStreamPacketLen = (int)pduReader.ReadUInt32() + 6;

                                    //Bump down the packet by this much
                                    readPtr += bytesPopped;
                                    readQueueLeft -= bytesPopped;
                                }

                                if (readStream.Length + readQueueLeft < readStreamPacketLen)
                                {
                                    //Not enough to finish off a packet.  Throw it on the queue and keep going.
                                    readStream.Write(readQueue, readPtr, readQueueLeft);
                                }
                                else
                                {
                                    //Finish off the packet and process it!
                                    int bytesPopped = (int)(readStreamPacketLen - readStream.Length);
                                    readStream.Write(readQueue, readPtr, bytesPopped);
                                    readStream.Seek(0, SeekOrigin.Begin);
                                    ProcessPacket(readStream);
                                    readStream = null;

                                    if (readQueueLeft > bytesPopped)
                                    {
                                        //Still more data in there... loop back around!
                                        readQueueLeft -= bytesPopped;
                                        readPtr += bytesPopped;
                                        needLoop = true;
                                    }
                                }
                            }
                        }
                        else if (readQueueLeft > 0)
                        {
                            //Starting fresh
                            if (readQueueLeft < 6)
                            {
                                //Not enough data to make a packet. Add to the pending stream and keep reading...
                                readStream = new MemoryStream();
                                readStream.Write(readQueue, readPtr, readQueueLeft);
                                readStreamPacketLen = 0;
                            }
                            else
                            {
                                //Check packet length to see if we have all of it
                                int packLen = MSBSwapper.SwapDW(BitConverter.ToInt32(readQueue, 2 + readPtr)) + 6;
                                if (readQueueLeft < packLen)
                                {
                                    //don't have all of it yet. Start a stream.
                                    readStream = new MemoryStream();
                                    readStream.Write(readQueue, readPtr, readQueueLeft);
                                    readStreamPacketLen = packLen;
                                }
                                else
                                {
                                    //We have the whole packet! Process it!
                                    MemoryStream packet = new MemoryStream(readQueue, readPtr, packLen);
                                    ProcessPacket(packet);

                                    if (packLen < readQueueLeft)
                                    {
                                        //More data on the stream.  Pop the last packet off and try again.
                                        readQueueLeft -= packLen;
                                        readPtr += packLen;
                                        needLoop = true;
                                    }
                                }
                            }
                        }
                    } while (needLoop);
                }
            }
            catch (SocketException e)
            {
                logger.Log(LogLevel.Error, "MonitorConn SocketException: " + e);
                //closed! or something.
            }
            catch (ThreadAbortException)
            {
                //Time to quit!
            }
            IsConnected = false;
        }

        private void ProcessPacket(MemoryStream packet)
        {
            //Grab the packet...
            SwappableBinaryReader pduReader = new SwappableBinaryReader(packet, true);

            byte pduType = pduReader.ReadByte();
            pduReader.ReadByte();   //reserved
            uint pduLen = pduReader.ReadUInt32();

            switch ((PDUType)pduType)
            {
                case PDUType.ASSOCIATE_RQ:
                    logger.Log(LogLevel.Info, "Received ASSOCIATE-RQ");

                    ParseAssociation(pduReader);

                    if (AssociationRequested != null)
                        AssociationRequested(this);
                    else
                    {
                        SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderACSE, AssociateRJReasons.NoReasonGiven);
                        CloseConnection();
                    }
                    break;
                case PDUType.ASSOCIATE_AC:
                    logger.Log(LogLevel.Info, "Received ASSOCIATE-AC");

                    ParseAssociation(pduReader);

                    if (AssociationAccepted != null)
                    {
                        Associated = true;
                        AssociationAccepted(this);
                    }
                    else
                        CloseConnection();
                    break;
                case PDUType.ASSOCIATE_RJ:
                    logger.Log(LogLevel.Info, "Received ASSOCIATE-RJ");

                    if (AssociationRejected != null)
                    {
                        //parse out types
                        pduReader.ReadByte();   //reserved
                        AssociateRJResults result = (AssociateRJResults)pduReader.ReadByte();
                        AssociateRJSources source = (AssociateRJSources)pduReader.ReadByte();
                        AssociateRJReasons reason = (AssociateRJReasons)pduReader.ReadByte();

                        AssociationRejected(this, result, source, reason);
                    }
                    CloseConnection();
                    break;

                case PDUType.P_DATA_TF:
                    //PS 3.8, Page 41
                    while (pduReader.BaseStream.Position < pduReader.BaseStream.Length)
                    {
                        uint itemLength = pduReader.ReadUInt32();
                        byte presentationContext = pduReader.ReadByte();
                        ActivePresentationContext = PresentationContexts[presentationContext];

                        //PS 3.8 Annex E, Pages 50-51
                        byte messageControlHeader = pduReader.ReadByte();
                        bool commandPacket = (messageControlHeader & 1) > 0;
                        bool lastFragment = (messageControlHeader & 2) > 0;

                        if (verboseLogging)
                        {
                            logger.Log(LogLevel.Debug, "Parsing P-DATA-TF. Length: " + itemLength + ", Pres: " + presentationContext + ", MCH: " + messageControlHeader);
                        }

                        //add the data to the buffer 
                        int subLength = (int)(itemLength - 2);
                        if (commandPacket)
                            commandStream.Write(pduReader.ReadBytes(subLength), 0, subLength);
                        else
                            dataStream.Write(pduReader.ReadBytes(subLength), 0, subLength);

                        if (lastFragment)
                        {
                            if (commandPacket)
                            {
                                //Note: It's possible that the entirety of the command set may be implicit vr little endian, but the spec is
                                //somewhat unclear on dealing with non-standard command set elements so i'll keep parsing it like this for now
                                commandDICOM = new DICOMData();
                                commandStream.Seek(0, SeekOrigin.Begin);
                                commandDICOM.ParseStream(commandStream, ActivePresentationContext.TransferSyntaxAccepted, false, true, logger);
                                commandStream = new MemoryStream();

                                if (verboseLogging)
                                {
                                    logger.Log(LogLevel.Debug, "Command DICOM:");
                                    logger.Log(LogLevel.Debug, commandDICOM.Dump());
                                }

                                //new command, so nuke the dataset (pretty sure this is right)
                                dataDICOM = null;

                                //no data packet attached, so process the command
                                if (commandDICOM.Elements.ContainsKey(DICOMTags.DataSetType) &&
                                    (ushort)commandDICOM.Elements[DICOMTags.DataSetType].Data == (ushort)DataSetTypes.NoDataSet)
                                    ProcessCommand();
                            }
                            else
                            {
                                dataDICOM = new DICOMData();
                                dataStream.Seek(0, SeekOrigin.Begin);
                                dataDICOM.ParseStream(dataStream, ActivePresentationContext.TransferSyntaxAccepted, false, true, logger);
                                dataStream = new MemoryStream();

                                if (verboseLogging)
                                {
                                    logger.Log(LogLevel.Debug, "Data DICOM:");
                                    logger.Log(LogLevel.Debug, commandDICOM.Dump());
                                }

                                if (commandDICOM != null)
                                    ProcessCommand();
                            }
                        }
                    }
                    break;

                case PDUType.RELEASE_RQ:
                    logger.Log(LogLevel.Info, "Received RELEASE-RQ...");
                    SendReleaseRP();
                    Released = true;
                    CloseConnection();
                    break;
                case PDUType.RELEASE_RP:
                    logger.Log(LogLevel.Info, "Received RELEASE-RP");
                    CloseConnection();
                    break;

                case PDUType.A_ABORT:
                    logger.Log(LogLevel.Warning, "Received A-ABORT!");
                    CloseConnection();
                    break;

                default:
                    //No idea!
                    break;
            }
        }

        private void ParseAssociation(SwappableBinaryReader pduReader)
        {
            //PS 3.8, Pages 34-43
            short protocolVersion = pduReader.ReadInt16();
            short reservedShort = pduReader.ReadInt16();

            if (verboseLogging)
            {
                logger.Log(LogLevel.Warning, "Parsed Protocol Version: " + protocolVersion);
            }

            byte[] calledAEBytes = pduReader.ReadBytes(16);
            CalledAE = Encoding.ASCII.GetString(calledAEBytes).Replace('\0', ' ').Trim();
            byte[] callingAEBytes = pduReader.ReadBytes(16);
            CallingAE = Encoding.ASCII.GetString(callingAEBytes).Replace('\0', ' ').Trim();

            if (verboseLogging)
            {
                logger.Log(LogLevel.Warning, "Parsed Called AE: " + CalledAE + ", Calling AE: " + CallingAE);
            }

            byte[] reserved32 = pduReader.ReadBytes(32);

            //Save association bytes for the AC message
            associationBytes = new byte[64];
            Array.Copy(calledAEBytes, 0, associationBytes, 0, 16);
            Array.Copy(callingAEBytes, 0, associationBytes, 16, 16);
            Array.Copy(reserved32, 0, associationBytes, 32, 32);

            //Variable Items:
            while (pduReader.BaseStream.Position < pduReader.BaseStream.Length)
            {
                byte itemType = pduReader.ReadByte();
                byte itemReserved = pduReader.ReadByte();
                ushort itemLength = pduReader.ReadUInt16();

                if (itemType == 0x20 || itemType == 0x21)   //presentation contexts
                {
                    PresentationContext context = new PresentationContext(pduReader, itemLength);
                    if (itemType == 0x20)   //if this is an associate-RQ...
                    {
                        PresentationContexts[context.ContextID] = context;
                        presentationContextLookupBySyntax[context.AbstractSyntaxSpecified] = context;

                        if (verboseLogging)
                        {
                            logger.Log(LogLevel.Warning, "Parsed Presentation Context: ID: " + context.ContextID + ": " + context.AbstractSyntaxSpecified);
                            foreach (var ts in context.TransferSyntaxesProposed)
                            {
                                logger.Log(LogLevel.Warning, "* Transfer Syntax: " + ts.ToString());
                            }
                        }

                        if (SupportedAbstractSyntaxes.Contains(context.AbstractSyntaxSpecified))
                        {
                            //Supported abstract, so negotiate transfer syntax

                            context.Result = PresentationResult.TransferSyntaxesNotSupported;

                            // Accept the first proposed one that's not unsupported!
                            foreach (TransferSyntax syntax in context.TransferSyntaxesProposed)
                            {
                                if (!TransferSyntaxes.unsupportedSyntaxes.Contains(syntax))
                                {
                                    if (syntax == TransferSyntaxes.ExplicitVRLittleEndian && context.TransferSyntaxesProposed.Any(ts => ts != TransferSyntaxes.ExplicitVRLittleEndian && !TransferSyntaxes.unsupportedSyntaxes.Contains(ts)))
                                    {
                                        // If this syntax is Explicit VR, but they list any other supported syntax as an option, skip it.
                                        continue;
                                    }

                                    context.TransferSyntaxAccepted = syntax;
                                    context.Result = PresentationResult.Acceptance;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            //Unsupported abstract!
                            context.Result = PresentationResult.AbstractSyntaxNotSupported;
                        }
                    }
                    else
                    {
                        //Associate-AC -- pull out result and transfer syntax and place in existing context
                        PresentationContext existingContext = PresentationContexts[context.ContextID];
                        existingContext.Result = context.Result;
                        if (existingContext.Result == PresentationResult.Acceptance)
                        {
                            existingContext.TransferSyntaxAccepted = context.TransferSyntaxesProposed[0];
                        }

                        if (verboseLogging)
                        {
                            logger.Log(LogLevel.Warning, "Parsed Presentation Context: ID: " + context.ContextID + ": " + existingContext.AbstractSyntaxSpecified + ", Result: " + context.Result + ", Accepted Transfer Syntax: " + existingContext.TransferSyntaxAccepted);
                        }
                    }
                }
                else if (itemType == 0x50)  //user info
                {
                    //PS 3.8 Annex D, Page 48

                    long startOffset = pduReader.BaseStream.Position;

                    while (pduReader.BaseStream.Position - startOffset < itemLength)
                    {
                        //read sub-item
                        byte subItemType = pduReader.ReadByte();
                        byte subItemReserved = pduReader.ReadByte();
                        ushort subItemLength = pduReader.ReadUInt16();

                        if (subItemType == 0x51)
                        {
                            OtherMaxPDU = pduReader.ReadUInt32();
                            if (OtherMaxPDU == 0)
                                OtherMaxPDU = 0xFFFFFFFF;

                            if (verboseLogging)
                            {
                                logger.Log(LogLevel.Warning, "Parsed Max PDU Size: " + OtherMaxPDU);
                            }
                        }
                        else
                        {
                            //no idea what it is, or we don't care
                            pduReader.ReadBytes(subItemLength);
                        }
                    }
                }
                else
                {
                    //no idea what it is, or we don't care
                    pduReader.ReadBytes(itemLength);
                }
            }
        }

        /// <summary>
        /// Send an association request (Associate-RQ) on the open DICOM channel.  Make sure that you've filled in the <see cref="PresentationContexts"/> list before calling this.
        /// </summary>
        public void SendAssociateRQ()
        {
            logger.Log(LogLevel.Info, "Sending ASSOCIATE-RQ...");

            if (PresentationContexts.Count == 0)
                throw new Exception("No presentation contexts!");

            //PS 3.8, Pages 34-36
            PDUBuilder pdu = new PDUBuilder(PDUType.ASSOCIATE_RQ);

            pdu.Write((ushort)1);  //protocol version
            pdu.Write((ushort)0);  //reserved - 0

            pdu.Write(StringUtil.ConvertAEToBytes(CalledAE));
            pdu.Write(StringUtil.ConvertAEToBytes(CallingAE));
            pdu.Write(new byte[32]);

            if (verboseLogging)
            {
                logger.Log(LogLevel.Warning, "Called AE: " + CalledAE + ", Calling AE: " + CallingAE);
            }

            //items

            //Application Context - mandatory -- PS 3.7, page 83
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.ApplicationContext);

                item.Write(ApplicationContextUID);

                pdu.Write(item.Build());
            }

            //presentation contexts - PS 3.8, Page 35
            foreach (PresentationContext context in PresentationContexts.Values)
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.PresentationContext);

                item.Write(context.ContextID); //presentation context id
                item.Write((byte)0);     //reserved - 0
                item.Write((byte)0);     //reserved - 0
                item.Write((byte)0);     //reserved - 0

                //abstract syntax - PS 3.8, Page 36
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.AbstractSyntax);

                    subitem.Write(context.AbstractSyntaxSpecified);

                    item.Write(subitem.Build());
                }

                if (verboseLogging)
                {
                    logger.Log(LogLevel.Warning, "Sending Presentation Context: ID: " + context.ContextID + ": " + context.AbstractSyntaxSpecified);
                }

                //transfer syntaxes - PS 3.8, Page 36
                foreach (TransferSyntax syntax in context.TransferSyntaxesProposed)
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.TransferSyntax);

                    subitem.Write(syntax);

                    item.Write(subitem.Build());

                    if (verboseLogging)
                    {
                        logger.Log(LogLevel.Warning, "* Transfer Syntax: " + syntax);
                    }
                }

                pdu.Write(item.Build());
            }

            //user info - PS 3.8, Page 37
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.UserInfo);

                //sub-items -- PS 3.8 Annex D, Page 48

                //max length sub-item
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.MaxPDULength);
                    subitem.Write((uint)MyMaxPDU);    //my max PDU size
                    item.Write(subitem.Build());

                    if (verboseLogging)
                    {
                        logger.Log(LogLevel.Warning, "Sending Max PDU: " + MyMaxPDU);
                    }
                }

                //implementation class uid sub-item - PS 3.7, Page 94
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.ImplementationUID);
                    subitem.Write(PACSsoftUID);
                    item.Write(subitem.Build());
                }

                //implementation version name sub-item - PS 3.7, Page 95
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.ImplementationName);
                    string descName = PACSsoftUID.Desc;
                    // 1 to 16 chars!
                    if (descName.Length < 1)
                    {
                        descName = "FillMeIn";
                    }
                    if (descName.Length > 16)
                    {
                        descName = descName.Substring(0, 16);
                    }
                    subitem.Write(Encoding.ASCII.GetBytes(descName));
                    item.Write(subitem.Build());
                }

                pdu.Write(item.Build());
            }

            SendData(pdu.Build());
        }

        /// <summary>
        /// Accepts an association request.  Make sure to accept presentation contexts before calling this.
        /// </summary>
        public void SendAssociateAC()
        {
            logger.Log(LogLevel.Info, "Sending ASSOCIATE-AC...");

            if (associationBytes == null)
                throw new Exception("No association request was parsed -- can't accept!");

            //PS 3.8, Pages 37-39
            PDUBuilder pdu = new PDUBuilder(PDUType.ASSOCIATE_AC);

            pdu.Write((ushort)1);  //protocol version
            pdu.Write((ushort)0);  //reserved - 0

            pdu.Write(associationBytes); //saved association bytes from the request

            //Application Context - mandatory -- PS 3.7, page 83
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.ApplicationContext);

                item.Write(ApplicationContextUID);

                pdu.Write(item.Build());
            }

            //presentation contexts
            foreach (PresentationContext context in PresentationContexts.Values)
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.PresentationContextReply);

                item.Write(context.ContextID); //presentation context id
                item.Write((byte)0);     //reserved - 0
                item.Write((byte)context.Result);     //context negotiation result
                item.Write((byte)0);     //reserved - 0

                if (context.Result == PresentationResult.Acceptance)
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.TransferSyntax);
                    subitem.Write(context.TransferSyntaxAccepted);
                    item.Write(subitem.Build());
                }

                pdu.Write(item.Build());

                if (verboseLogging)
                {
                    logger.Log(LogLevel.Warning, "Sending Presentation Context: ID: " + context.ContextID + ": " + context.AbstractSyntaxSpecified + ", Result: " + context.Result + ", Accepted Transfer Syntax: " + context.TransferSyntaxAccepted);
                }
            }

            //user info item - Page 39
            {
                AssociationItemBuilder item = new AssociationItemBuilder(AssociationItemType.UserInfo);

                //sub-items -- PS 3.8 Annex D, Page 48

                //max length sub-item
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.MaxPDULength);
                    subitem.Write((uint)MyMaxPDU);    //my max PDU size
                    item.Write(subitem.Build());

                    if (verboseLogging)
                    {
                        logger.Log(LogLevel.Warning, "Sending Max PDU: " + MyMaxPDU);
                    }
                }

                //implementation class uid sub-item - PS 3.7, Page 94
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.ImplementationUID);
                    subitem.Write(PACSsoftUID);
                    item.Write(subitem.Build());
                }

                //implementation version name sub-item - PS 3.7, Page 95
                {
                    AssociationItemBuilder subitem = new AssociationItemBuilder(AssociationItemType.ImplementationName);
                    string descName = PACSsoftUID.Desc;
                    // 1 to 16 chars!
                    if (descName.Length < 1)
                    {
                        descName = "FillMeIn";
                    }
                    if (descName.Length > 16)
                    {
                        descName = descName.Substring(0, 16);
                    }
                    subitem.Write(Encoding.ASCII.GetBytes(descName));
                    item.Write(subitem.Build());
                }

                pdu.Write(item.Build());
            }

            SendData(pdu.Build());
        }

        /// <summary>
        /// Reject an association request (Associate-RJ).
        /// </summary>
        /// <param name="result">DICOM rejection type</param>
        /// <param name="source">DICOM rejection source</param>
        /// <param name="reason">DICOM rejection reason</param>
        public void SendAssociateRJ(AssociateRJResults result, AssociateRJSources source, AssociateRJReasons reason)
        {
            logger.Log(LogLevel.Info, "Sending ASSOCIATE-RJ (Result: " + result + ", Source: " + source + ", Reason: " + reason + ")...");

            //PS 3.8, Page 40
            PDUBuilder builder = new PDUBuilder(PDUType.ASSOCIATE_RJ);

            builder.Write((byte)0);  //reserved - 0
            builder.Write((byte)result);
            builder.Write((byte)source);
            builder.Write((byte)reason);

            SendData(builder.Build());
        }

        /// <summary>
        /// Request a connection release (sends a Release-RQ).
        /// </summary>
        public void SendReleaseRQ()
        {
            logger.Log(LogLevel.Info, "Sending RELEASE-RQ...");

            //PS 3.8, Page 42
            PDUBuilder pdu = new PDUBuilder(PDUType.RELEASE_RQ);

            pdu.Write((uint)0);  //reserved - 0

            SendData(pdu.Build());

            Released = true;
        }

        /// <summary>
        /// Acknowledge a release request (sends a Release-RP).
        /// </summary>
        public void SendReleaseRP()
        {
            logger.Log(LogLevel.Info, "Sending RELEASE-RP...");

            //PS 3.8, Page 42
            PDUBuilder pdu = new PDUBuilder(PDUType.RELEASE_RP);

            pdu.Write((uint)0);  //reserved - 0

            SendData(pdu.Build());
        }

        /// <summary>
        /// Send an A-Abort command, which (hopefully) cancels the pending commend.
        /// </summary>
        /// <param name="source">DICOM abort source</param>
        /// <param name="reason">DICOM abort reason</param>
        public void SendAAbort(AAbortSources source, AAbortReasons reason)
        {
            logger.Log(LogLevel.Info, "Sending A-ABORT...");

            //PS 3.8, Page 43
            PDUBuilder pdu = new PDUBuilder(PDUType.A_ABORT);

            pdu.Write((byte)0);  //reserved - 0
            pdu.Write((byte)0);  //reserved - 0
            pdu.Write((byte)source);
            pdu.Write((byte)reason);

            SendData(pdu.Build());
        }

        private void SendPDATATF(DICOMData data, bool command)
        {
            if (verboseLogging)
            {
                logger.Log(LogLevel.Debug, "Sending P-DATA-TF. Command: " + command);

                logger.Log(LogLevel.Debug, data.Dump());
            }

            //serialize dicom stream to memory with the current transfer syntax
            MemoryStream dicomStream = new MemoryStream();
            data.WriteStream(dicomStream, logger, ActivePresentationContext.TransferSyntaxAccepted, true);
            byte[] dicomBuffer = dicomStream.ToArray();

            //Now form the packets
            int maxPackLen = (int)(OtherMaxPDU - 12);
            int packetCount = (int)(dicomBuffer.Length / maxPackLen);

            //packetCount is number of maxed packets, so actual count of packets is + 1. Works out well for the array, though.
            for (int i = 0; i <= packetCount; i++)
            {
                //PS 3.8, Page 41
                PDUBuilder pdu = new PDUBuilder(PDUType.P_DATA_TF);

                //PS 3.8 Annex E, Pages 50-51
                pdu.Write((uint)0);  //temp seg-len
                pdu.Write(ActivePresentationContext.ContextID);  //presentation context id
                pdu.Write((byte)((command ? 1 : 0) | ((i == packetCount) ? 2 : 0)));  //message control header

                if (i < packetCount)    //middle packet
                    pdu.Write(dicomBuffer, i * maxPackLen, maxPackLen);
                else  //last packet
                    pdu.Write(dicomBuffer, i * maxPackLen, dicomBuffer.Length - i * maxPackLen);

                //fill out the segment length
                pdu.BaseStream.Seek(6, SeekOrigin.Begin);
                pdu.Write((uint)(pdu.BaseStream.Length - 10));

                SendData(pdu.Build());
            }
        }

        private ushort GenMessageID()
        {
            //return and increment message id
            return nextMessageID++;
        }

        private byte GenPresentationContextID()
        {
            //return and increment presentation context id
            byte toret = nextPresentationContextID;
            nextPresentationContextID += 2;
            return toret;
        }

        /// <summary>
        /// Add a presentation context to the presentation contexts list.
        /// </summary>
        /// <param name="context">The context to add</param>
        public void AddPresentationContext(PresentationContext context)
        {
            context.ContextID = GenPresentationContextID();
            PresentationContexts[context.ContextID] = context;
            presentationContextLookupBySyntax[context.AbstractSyntaxSpecified] = context;
        }

        /// <summary>
        /// A helper method for when you're adding assorted abstract and transfer syntaxes to a list for sending.  Makes sure the combo is
        /// supported by the connection for sending without adding duplicates.
        /// </summary>
        /// <param name="abstractSyntax"></param>
        /// <param name="transferSyntax"></param>
        public void EnsureAbstractAndTransferSyntaxesHandled(AbstractSyntax abstractSyntax, TransferSyntax transferSyntax)
        {
            if (transferSyntax == TransferSyntaxes.ExplicitVRLittleEndian)
            {
                transferSyntax = TransferSyntaxes.ImplicitVRLittleEndian;
            }

            foreach (var context in PresentationContexts)
            {
                if (context.Value.AbstractSyntaxSpecified == abstractSyntax)
                {
                    if (!context.Value.TransferSyntaxesProposed.Contains(transferSyntax))
                    {
                        context.Value.TransferSyntaxesProposed.Add(transferSyntax);
                    }
                    return;
                }
            }

            AddPresentationContext(new PresentationContext(abstractSyntax, new List<TransferSyntax> { transferSyntax }));
        }

        /// <summary>
        /// Call this helper function right before you start a connection up, and it will ensure that all abstract syntaxes that are
        /// decompressible by DICOMSharp have at least implicit VR little endian on them as a fallback to use.
        /// </summary>
        public void EnsureUsableTransferSyntaxesExist()
        {
            foreach (var context in PresentationContexts)
            {
                if (context.Value.TransferSyntaxesProposed.Contains(TransferSyntaxes.ImplicitVRLittleEndian))
                {
                    continue;
                }

                if (context.Value.TransferSyntaxesProposed.Any(trans => CompressionWorker.SupportsDecompression(trans.Compression)))
                {
                    context.Value.TransferSyntaxesProposed.Add(TransferSyntaxes.ImplicitVRLittleEndian);
                }
            }
        }

        private void ProcessCommand()
        {
            if (commandDICOM == null)
            {
                logger.Log(LogLevel.Warning, "ProcessCommand with null commandDICOM");
                return;
            }

            if (!commandDICOM.Elements.ContainsKey(DICOMTags.CommandField))
            {
                logger.Log(LogLevel.Warning, "ProcessCommand with commandDICOM without CommandField tag");
                return;
            }

            PDATACommands command = (PDATACommands)(ushort)commandDICOM.Elements[DICOMTags.CommandField].Data;

            if (verboseLogging)
            {
                logger.Log(LogLevel.Debug, "Processing Command: " + command.ToString() + ", Data: " + (dataDICOM != null ? "Attached" : "None"));
            }

            if (getHandling && command == PDATACommands.CSTORERSP)
            {
                CommandStatus status = (CommandStatus)(ushort)commandDICOM[DICOMTags.Status].Data;
                if (status == CommandStatus.Success)
                    getCompleted++;
                else if (status == CommandStatus.Warning_DuplicateInvocation || status == CommandStatus.Warning_DuplicateSOPInstance)
                    getWarned++;
                else
                    getFailed++;

                SendNextGetFile();
            }

            if (CommandReceived != null)
                CommandReceived(this, command, commandDICOM, dataDICOM);
        }

        /// <summary>
        /// Send an echo request (C-ECHO-RQ, basically the DICOM equivalent of a PING).
        /// </summary>
        public void SendCECHORQ()
        {
            logger.Log(LogLevel.Info, "Sending C-ECHO-RQ...");

            //set new active pres context
            if (this._supportsAbstractContext(AbstractSyntaxes.VerificationSOPClass))
                ActivePresentationContext = presentationContextLookupBySyntax[AbstractSyntaxes.VerificationSOPClass];
            else
                throw new Exception("No verification syntax in association...");

            //PS 3.7, Page 44
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = ActivePresentationContext.AbstractSyntaxSpecified.UidStr;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CECHORQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Respond to a C-ECHO-RQ (send a C-ECHO-RSP).  This is responding to the DICOM ping.
        /// </summary>
        public void SendCECHORSP()
        {
            //PS 3.7, Page 45
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.AffectedSOPClass].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CECHORSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)CommandStatus.Success;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Attempt to store a DICOM dataset onto the target by sending a C-STORE-RQ packet.
        /// This is documented in the DICOM speec under PS 3.7, pages 32-33.
        /// </summary>
        /// <param name="storeData">The dataset to send/store</param>
        /// <param name="syntax">Which abstract syntax to use for the transfer (compressed images or not, etc.)</param>
        /// <param name="priority">DICOM priority level for the transfer</param>
        /// <param name="moveOriginatorAE">If this store request was initiated by a C-MOVE-RQ, this is the AE title of the SCU that requested the C-MOVE.  If this wasn't
        /// started by a C-MOVE, then leave this parameter null.</param>
        /// <param name="moveOriginatorMessageID">If this store request was initiated by a C-MOVE-RQ, this is the message ID from the original C-MOVE-RQ's command data set.
        /// If this wasn't started by a C-MOVE, then leave this parameter null.</param>
        public void SendCSTORERQ(DICOMData storeData, AbstractSyntax syntax, CommandPriority priority, string moveOriginatorAE, ushort moveOriginatorMessageID)
        {
            logger.Log(LogLevel.Info, "Sending C-STORE-RQ...");

            //set new active pres context
            if (this._supportsAbstractContext(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else if (this._supportsAbstractContext(AbstractSyntaxes.CTImageStorage))
                ActivePresentationContext = presentationContextLookupBySyntax[AbstractSyntaxes.CTImageStorage];
            else
                throw new Exception("No matching syntax (" + syntax.UidStr + ") in association...");

            //PS 3.7, Pages 32-33
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = ActivePresentationContext.AbstractSyntaxSpecified;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CSTORERQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.Priority].Data = (ushort)priority;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.DataSetExists;
            cmd[DICOMTags.AffectedSOPInstanceUID].Data = storeData[DICOMTags.SOPInstanceUID].Data;

            //move originator stuff if necessary
            if (moveOriginatorAE != null)
            {
                cmd[DICOMTags.MoveOriginatorAETitle].Data = moveOriginatorAE;
                cmd[DICOMTags.MoveOriginatorMessageID].Data = moveOriginatorMessageID;
            }

            SendPDATATF(cmd, true);

            SendPDATATF(storeData, false);
        }

        /// <summary>
        /// Reply to a C-STORE-RQ with a C-STORE-RSP.
        /// </summary>
        /// <param name="status">The DICOM status from the store request (whether it worked or not)</param>
        public void SendCSTORERSP(CommandStatus status)
        {
            //PS 3.7, Page 33
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.AffectedSOPClass].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CSTORERSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)status;
            cmd[DICOMTags.AffectedSOPInstanceUID].Data = commandDICOM[DICOMTags.AffectedSOPInstanceUID].Data;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Send a C-FIND request (C-FIND-RQ) to look up available datasets.
        /// The rules for what to send here are outlined in the DICOM spec in PS 3.7, page 34.
        /// </summary>
        /// <param name="priority">The DICOM priority of the find request</param>
        /// <param name="level">Which level (patient or study) that the query is at</param>
        /// <param name="data">The query parameters to query with</param>
        public void SendCFINDRQ(CommandPriority priority, QueryRetrieveLevel level, DICOMData data)
        {
            logger.Log(LogLevel.Info, "Sending C-FIND-RQ...");

            //set new active pres context
            AbstractSyntax syntax = null;
            if (level == QueryRetrieveLevel.StudyRoot) syntax = AbstractSyntaxes.StudyRootQueryRetrieveInformationModelFIND;
            else if (level == QueryRetrieveLevel.PatientRoot) syntax = AbstractSyntaxes.PatientRootQueryRetrieveInformationModelFIND;
            else if (level == QueryRetrieveLevel.ModalityWorklist) syntax = AbstractSyntaxes.ModalityWorklistInformationModelFIND;
            else throw new NotSupportedException("Level " + level + " not supported!");

            if (this._supportsAbstractContext(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else
                throw new Exception("No matching syntax (" + syntax + ") in association...");

            //PS 3.7, Page 34
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = ActivePresentationContext.AbstractSyntaxSpecified;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CFINDRQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.Priority].Data = (ushort)priority;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.DataSetExists;

            SendPDATATF(cmd, true);

            SendPDATATF(data, false);
        }

        /// <summary>
        /// Respond to a C-FIND request (send a C-FIND-RSP) with results of any sort, including that there are no results.
        /// How to properly respond to a C-FIND request are outlined in the DICOM spec in PS 3.7, page 35.
        /// </summary>
        /// <param name="data">The DICOM dataset with the response parameters.  If there's no data to send, then this parameter can be null.</param>
        /// <param name="status">The DICOM status of the response (whether or not there is more data coming, etc.)</param>
        public void SendCFINDRSP(DICOMData data, CommandStatus status)
        {
            logger.Log(LogLevel.Debug, "Sending C-FIND-RSP (" + (data == null ? "Final" : "Data") + ")...");

            //PS 3.7, Page 35
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.AffectedSOPClass].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CFINDRSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)(data == null ? DataSetTypes.NoDataSet : DataSetTypes.DataSetExists);
            cmd[DICOMTags.Status].Data = (ushort)status;

            SendPDATATF(cmd, true);

            if (data != null)
                SendPDATATF(data, false);
        }

        /// <summary>
        /// Respond to a C-FIND request with a <see cref="QRResponseData"/> structure pre-filled with response data.
        /// </summary>
        /// <param name="response">A filled out structure with data to respond to the FIND-RQ with.</param>
        public void SendCFINDRSP(QRResponseData response)
        {
            foreach (Dictionary<uint, object> respRow in response.ResponseRows)
            {
                DICOMData data = new DICOMData();
                foreach (uint tag in respRow.Keys)
                    data[tag].Data = respRow[tag];

                SendCFINDRSP(data, CommandStatus.Pending_AllOptionalKeysReturned);
            }
            SendCFINDRSP(null, CommandStatus.Success);
        }

        /// <summary>
        /// Send a request to get a DICOM dataset (C-GET-RQ) from the SCP.
        /// The parameters for this request are outlined in the DICOM spec in PS 3.7, pages 37-38.
        /// </summary>
        /// <param name="priority">The DICOM priority for the request</param>
        /// <param name="level">The query/retrieve level for the request (study, patient)</param>
        /// <param name="data">The DICOM dataset outlining the request</param>
        public void SendCGETRQ(CommandPriority priority, QueryRetrieveLevel level, DICOMData data)
        {
            logger.Log(LogLevel.Info, "Sending C-GET-RQ...");

            //set new active pres context
            AbstractSyntax syntax = null;
            if (level == QueryRetrieveLevel.StudyRoot) syntax = AbstractSyntaxes.StudyRootQueryRetrieveInformationModelGET;
            else if (level == QueryRetrieveLevel.PatientRoot) syntax = AbstractSyntaxes.PatientRootQueryRetrieveInformationModelGET;
            else throw new NotSupportedException("Level " + level + " not supported!");

            if (this._supportsAbstractContext(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else
                throw new Exception("No matching syntax (" + syntax + ") in association...");

            //PS 3.7, Pages 37-38
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = ActivePresentationContext.AbstractSyntaxSpecified;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CGETRQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.Priority].Data = (ushort)priority;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.DataSetExists;

            SendPDATATF(cmd, true);

            SendPDATATF(data, false);
        }

        /// <summary>
        /// Respond to a C-GET request (send a C-GET-RSP) with information about the progress of the C-GET-RQ.
        /// The proper response methods are outlined in the DICOM spec in PS 3.7, pages 38-39.
        /// </summary>
        /// <param name="messageIDRepliedTo">The DICOM Message ID to reply to</param>
        /// <param name="status">The DICOM status of the response</param>
        /// <param name="remaining">How many data sets are remaining to be sent</param>
        /// <param name="completed">How many data sets have completed</param>
        /// <param name="failed">How many data sets were unable to be sent for some reason (failed)</param>
        /// <param name="warning">How many data sets there were warnings attempting to send</param>
        public void SendCGETRSP(ushort messageIDRepliedTo, CommandStatus status, ushort remaining, ushort completed, ushort failed, ushort warning)
        {
            //PS 3.7, Pages 38-39
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.AffectedSOPClass].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CGETRSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = messageIDRepliedTo;
            cmd[DICOMTags.DataSetType].Data = DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)status;

            cmd[DICOMTags.NumberOfRemainingSubOps].Data = remaining;
            cmd[DICOMTags.NumberOfCompletedSubOps].Data = completed;
            cmd[DICOMTags.NumberOfFailedSubOps].Data = failed;
            cmd[DICOMTags.NumberOfWarningSubOps].Data = warning;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Send a C-MOVE request (C-MOVE-RQ) for the SCP to send images to someone.
        /// How this works is outlined in the DICOM spec in PS 3.7, pages 41-42.
        /// </summary>
        /// <param name="priority">The DICOM priority of the request</param>
        /// <param name="level">The query/retrieve level of the request</param>
        /// <param name="moveTarget">The AE title of the move destination SCP that the connected SCP will connect and initiate C-STOREs to</param>
        /// <param name="data">The query parameters to query with</param>
        public void SendCMOVERQ(CommandPriority priority, QueryRetrieveLevel level, string moveTarget, DICOMData data)
        {
            logger.Log(LogLevel.Info, "Sending C-MOVE-RQ...");

            //set new active pres context
            AbstractSyntax syntax = null;
            if (level == QueryRetrieveLevel.StudyRoot) syntax = AbstractSyntaxes.StudyRootQueryRetrieveInformationModelMOVE;
            else if (level == QueryRetrieveLevel.PatientRoot) syntax = AbstractSyntaxes.PatientRootQueryRetrieveInformationModelMOVE;
            else throw new NotSupportedException("Level " + level + " not supported!");

            if (this._supportsAbstractContext(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else
                throw new Exception("No matching syntax (" + syntax + ") in association...");

            //PS 3.7, Pages 41-42
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = ActivePresentationContext.AbstractSyntaxSpecified;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CMOVERQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.Priority].Data = (ushort)priority;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.DataSetExists;
            cmd[DICOMTags.MoveDestination].Data = moveTarget;

            SendPDATATF(cmd, true);

            SendPDATATF(data, false);
        }

        /// <summary>
        /// Respond to a C-MOVE request (send a C-MOVE-RSP) with the status of the MOVE.
        /// This is outlined in the DICOM spec in PS 3.7, page 42.
        /// </summary>
        /// <param name="status">The DICOM status of the response</param>
        /// <param name="remaining">How many data sets are remaining to be sent</param>
        /// <param name="completed">How many data sets have completed</param>
        /// <param name="failed">How many data sets were unable to be sent for some reason (failed)</param>
        /// <param name="warning">How many data sets there were warnings attempting to send</param>
        public void SendCMOVERSP(CommandStatus status, ushort remaining, ushort completed, ushort failed, ushort warning)
        {
            //PS 3.7, Page 42
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.AffectedSOPClass].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CMOVERSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)status;

            cmd[DICOMTags.NumberOfRemainingSubOps].Data = remaining;
            cmd[DICOMTags.NumberOfCompletedSubOps].Data = completed;
            cmd[DICOMTags.NumberOfFailedSubOps].Data = failed;
            cmd[DICOMTags.NumberOfWarningSubOps].Data = warning;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Send a C-CANCEL-RQ, which will cancel a pending C-FIND-RQ, C-GET-RQ, or C-MOVE-RQ.
        /// This is outlined in the DICOM spec in PS 3.7, pages 36, 39, and 43.
        /// </summary>
        public void SendCCANCELRQ()
        {
            logger.Log(LogLevel.Info, "Sending C-CANCEL-RQ...");

            //PS 3.7, Page 36 - FIND
            //PS 3.7, Page 39 - GET
            //PS 3.7, Page 43 - MOVE

            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.CCANCELRQ;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Send an N-GET-RQ to the SCP.
        /// This is outlined in the DICOM spec in PS 3.7, page 62.
        /// </summary>
        /// <param name="syntax">The abstract syntax for this request to use.</param>
        /// <param name="requestedSopClassUid">The SOP class UID to request</param>
        /// <param name="requestedSopInstanceUid">The SOP instance UID to request</param>
        /// <param name="attributesRequested">A list of DICOM tags to request</param>
        public void SendNGETRQ(AbstractSyntax syntax, string requestedSopClassUid, string requestedSopInstanceUid, uint[] attributesRequested)
        {
            //RxIMP: I don't really understand the N-GET stuff at all...  So this is mostly filler for now.

            logger.Log(LogLevel.Info, "Sending N-GET-RQ...");

            //set new active pres context
            if (this._supportsAbstractContext(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else
                throw new Exception("No matching syntax in association...");

            //PS 3.7, Page 62
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.RequestedSOPClassUID].Data = requestedSopClassUid;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.NGETRQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.RequestedSOPInstanceUID].Data = requestedSopInstanceUid;
            cmd[DICOMTags.AttributeIdentifierList].Data = attributesRequested;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Respond to an N-GET request (sends an N-GET-RSP.)
        /// This is outlined in the DICOM spec in PS 3.7, page 63.
        /// </summary>
        /// <param name="status">The DICOM status for the response</param>
        /// <param name="data">The data elements to respond with (should match what was requested)</param>
        public void SendNGETRSP(CommandStatus status, DICOMData data)
        {
            //RxIMP: I don't really understand the N-GET stuff at all...  So this is mostly filler for now.

            //PS 3.7, Page 63
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.RequestedSOPClassUID].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.NGETRSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = data != null ? (ushort)DataSetTypes.DataSetExists : (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)status;
            cmd[DICOMTags.AffectedSOPInstanceUID].Data = commandDICOM[DICOMTags.RequestedSOPInstanceUID].Data;

            SendPDATATF(cmd, true);
            SendPDATATF(data, false);
        }

        /// <summary>
        /// Send an N-ACTION-RQ to the SCP.
        /// This is outlined in the DICOM spec in PS 3.7, page 66.
        /// </summary>
        /// <param name="syntax">The abstract syntax for this request to use.</param>
        /// <param name="requestedInstance">The SOP instance UID to request</param>
        /// <param name="actionTypeID">The action type ID for the N-ACTION</param>
        public void SendNACTIONRQ(AbstractSyntax syntax, string requestedInstance, ushort actionTypeID)
        {
            //RxIMP: I don't really understand the N-ACTION stuff at all...  So this is mostly filler for now.

            logger.Log(LogLevel.Info, "Sending N-ACTION-RQ...");

            //set new active pres context
            if (presentationContextLookupBySyntax.ContainsKey(syntax))
                ActivePresentationContext = presentationContextLookupBySyntax[syntax];
            else
                throw new Exception("No matching syntax in association...");

            //PS 3.7, Page 66
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.RequestedSOPClassUID].Data = ActivePresentationContext.AbstractSyntaxSpecified.UidStr;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.NACTIONRQ;
            cmd[DICOMTags.MessageID].Data = GenMessageID();
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.RequestedSOPInstanceUID].Data = requestedInstance;
            cmd[DICOMTags.ActionTypeID].Data = actionTypeID;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Respond to an N-ACTION request (sends an N-ACTION-RSP.)
        /// This is outlined in the DICOM spec in PS 3.7, page 67.
        /// </summary>
        /// <param name="status">The DICOM status for the response</param>
        /// <param name="actionTypeID">The action type ID for the response</param>
        public void SendNACTIONRSP(CommandStatus status, ushort actionTypeID)
        {
            //RxIMP: I don't really understand the N-ACTION stuff at all...  So this is mostly filler for now.

            //PS 3.7, Page 67
            DICOMData cmd = new DICOMData();

            cmd[DICOMTags.AffectedSOPClass].Data = commandDICOM[DICOMTags.RequestedSOPClassUID].Data;
            cmd[DICOMTags.CommandField].Data = (ushort)PDATACommands.NACTIONRSP;
            cmd[DICOMTags.MessageIDRepliedTo].Data = commandDICOM[DICOMTags.MessageID].Data;
            cmd[DICOMTags.DataSetType].Data = (ushort)DataSetTypes.NoDataSet;
            cmd[DICOMTags.Status].Data = (ushort)status;
            cmd[DICOMTags.AffectedSOPInstanceUID].Data = commandDICOM[DICOMTags.RequestedSOPInstanceUID].Data;
            cmd[DICOMTags.ActionTypeID].Data = actionTypeID;

            SendPDATATF(cmd, true);
        }

        /// <summary>
        /// Use this function to handle a GET-RQ.  Fill out the response field with the images to respond with, and this will handle
        /// all of the get response process with the files you specify.
        /// </summary>
        /// <param name="response">A pre-filled structure of instances to send.</param>
        public void StartGetResponse(QRResponseData response)
        {
            getHandling = true;
            getResponse = response;
            getMessageID = (ushort)commandDICOM[DICOMTags.MessageID].Data;
            getCompleted = 0;
            getWarned = 0;
            getFailed = 0;

            SendNextGetFile();
        }

        private void SendNextGetFile()
        {
            if (getResponse.FilesToSend.Count == 0)
            {
                SendCGETRSP(getMessageID, CommandStatus.Success, 0, getCompleted, getFailed, getWarned);
                return;
            }

            SendCGETRSP(getMessageID, CommandStatus.Pending_AllOptionalKeysReturned, (ushort)getResponse.FilesToSend.Count, getCompleted, getFailed, getWarned);

            object toSendO = getResponse.FilesToSend.Dequeue();

            DICOMData toSend = null;
            if (toSendO is DICOMData)
                toSend = (DICOMData)toSendO;
            else if (toSendO is FileInfo)
            {
                toSend = new DICOMData();
                FileInfo fi = (FileInfo)toSendO;
                toSend.ParseFile(fi.FullName, true, logger);
            }

            AbstractSyntax absSyn = AbstractSyntaxes.CTImageStorage;
            if (toSend.Elements.ContainsKey(DICOMTags.SOPClassUID))
                absSyn = AbstractSyntaxes.Lookup((string)toSend[DICOMTags.SOPClassUID].Data);
            SendCSTORERQ(toSend, absSyn, CommandPriority.Medium, null, 0);
        }

        /// <summary>
        /// Use this function to handle a MOVE-RQ.  Fill out the response field with the images to respond with, and this will handle
        /// all of the MOVE process to the new target entity, including sending move responses to the original caller.
        /// </summary>
        /// <param name="targetEntity">A filled out ApplicationEntity structure with information about the move destination.</param>
        /// <param name="response">A response structure filled with the images to send.</param>
        public void StartMoveResponse(ApplicationEntity targetEntity, QRResponseData response)
        {
            moveSender = new DICOMSender(logger, this._connectionName + "-Mover", this.verboseLogging);
            moveSender.Send(new ApplicationEntity(CalledAE), targetEntity, response.FilesToSend, this.CallingAE, (ushort)commandDICOM[DICOMTags.MessageID].Data);
            moveSender.SendUpdate += new DICOMSender.SendUpdateHandler(moveSender_SendUpdate);
            moveSender.SCUFinished += new DICOMSCU.SCUFinishedHandler(moveSender_SCUFinished);
        }

        private void moveSender_SCUFinished(DICOMSCU scu, bool success)
        {
            moveSender = null;
        }

        private void moveSender_SendUpdate(DICOMSender sender, ushort remaining, ushort completed, ushort warned, ushort failed)
        {
            //There was a note in DICOMProvider to send update before updating the totals...
            //but that makes no sense -- it also sent 2 cmoversp's after the last image, one with the completed count too low...  so i think that's wrong...

            SendCMOVERSP(CommandStatus.Pending_AllOptionalKeysReturned, remaining, completed, failed, warned);
        }

        /// <summary>
        /// Log a line to the connection's logger
        /// </summary>
        /// <param name="logLevel">The logging level to use</param>
        /// <param name="logLine">The string to log</param>
        public void LogLine(LogLevel logLevel, string logLine)
        {
            this.logger.Log(logLevel, logLine);
        }

        private bool _supportsAbstractContext(AbstractSyntax syntax)
        {
            if (!this.presentationContextLookupBySyntax.ContainsKey(syntax))
            {
                return false;
            }

            return this.presentationContextLookupBySyntax[syntax].Result == PresentationResult.Acceptance;
        }

        /// <summary>
        /// Whether the current connection has successfully associated yet or not.
        /// </summary>
        public bool Associated { get; private set; }
        /// <summary>
        /// Whether the current connection has been released yet or not.
        /// </summary>
        public bool Released { get; private set; }

        /// <summary>
        /// The called AE (the AE of this SCP or of the target SCP) title to use
        /// </summary>
        public string CalledAE { get; set; }
        /// <summary>
        /// The calling AE (the AE of this SCU or of the target SCP) title to use
        /// </summary>
        public string CallingAE { get; set; }

        /// <summary>
        /// Set the Max PDU size for this DICOMConnection to use.  It defaults to 0xC80E, which is a commonly functional size.  You may need to set this
        /// to something else (usually smaller) for compatibility with odd legacy devices.
        /// </summary>
        public uint MyMaxPDU { get; set; }

        /// <summary>
        /// (read-only) The Max PDU size of the connected SCP/SCU.  This is given during association.
        /// </summary>
        public uint OtherMaxPDU { get; private set; }

        /// <summary>
        /// This is a list of all abstract syntaxes that will be supported by an SCP.  For an SCU, this array is not used.
        /// </summary>
        public HashSet<AbstractSyntax> SupportedAbstractSyntaxes { get; private set; }

        /// <summary>
        /// (read-only) The last time any data was sent or received by the DICOMConnection.
        /// </summary>
        public DateTime LastAction { get; private set; }

        /// <summary>
        /// Method delegate for handling a rejected association.
        /// </summary>
        /// <param name="conn">The DICOMConnection on which the rejection was received</param>
        /// <param name="result">The DICOM rejection results type</param>
        /// <param name="source">The DICOM rejection source type</param>
        /// <param name="reason">The DICOM rejection reason</param>
        public delegate void AssociationRejectedHandler(DICOMConnection conn, AssociateRJResults result, AssociateRJSources source, AssociateRJReasons reason);
        /// <summary>
        /// This event callback is called whenever an Association request by this SCU is rejected.
        /// </summary>
        public event AssociationRejectedHandler AssociationRejected;

        /// <summary>
        /// Method delegate for basic DICOMConnection callbacks that just need to identify which DICOMConnection the message came from.
        /// </summary>
        /// <param name="conn"></param>
        public delegate void BasicConnectionHandler(DICOMConnection conn);

        /// <summary>
        /// This event callback is called whenever an Association request is received by this SCP.
        /// </summary>
        public event BasicConnectionHandler AssociationRequested;
        /// <summary>
        /// This event callback is called whenever an Association request (initiated by this SCU) is accepted by the remote SCP.
        /// </summary>
        public event BasicConnectionHandler AssociationAccepted;
        /// <summary>
        /// This event callback is called whenever the connection is closed for any reason.
        /// </summary>
        public event BasicConnectionHandler ConnectionClosed;

        /// <summary>
        /// Method delegate for any P-DATA command type received by the DICOMConnection, since there is no default handling of
        /// any DICOM commands in DICOMConnection
        /// </summary>
        /// <param name="conn">The DICOMConnection the command was received on</param>
        /// <param name="command">The DICOM command type</param>
        /// <param name="cmdDICOM">The command parameters</param>
        /// <param name="dataDICOM">The data set (if any) attached to the DICOM command</param>
        public delegate void CommandHandler(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM);
        /// <summary>
        /// This event callback is called whenever a P-DATA command is received by the DICOMConnection, either as SCP or SCU
        /// </summary>
        public event CommandHandler CommandReceived;

        /// <summary>
        /// Keeps track of all Presentation Contexts for the connection.  Can be read for debugging purposes but change its contents
        /// at your own risk.
        /// </summary>
        public Dictionary<byte, PresentationContext> PresentationContexts { get; private set; }
        /// <summary>
        /// Keeps track of the current active presentation context for the connection.  The presentation context changes whenever
        /// a new command is received, so it needs to be tracked to know what to respond with.
        /// </summary>
        public PresentationContext ActivePresentationContext { get; private set; }

        /// <summary>
        /// Whether the Connection is currently connected to anything.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Private lookup for the presentation contexts by abstract syntax to know when to switch contexts.
        /// </summary>
        private Dictionary<AbstractSyntax, PresentationContext> presentationContextLookupBySyntax;

        private Socket RemoteSocket;

        /// <summary>
        /// The IP Endpoint at the remote end of the connection.s
        /// </summary>
        public IPEndPoint RemoteEndPoint { get { return (IPEndPoint)RemoteSocket.RemoteEndPoint; } }

        private ILogger logger;
        private string _connectionName;
        private bool verboseLogging;

        private byte[] associationBytes;
        private Thread monitorThread;
        private ushort nextMessageID;
        private byte nextPresentationContextID;

        private MemoryStream commandStream, dataStream;
        private DICOMData commandDICOM, dataDICOM;

        internal static Uid PACSsoftUID = new Uid("1.3.12.2.1107.5.4", "PACSSOFT");

        internal static Uid ApplicationContextUID = new Uid("1.2.840.10008.3.1.1.1", "DICOM Application Context Name");

        //stuff for C-GET handling
        private bool getHandling;
        private ushort getMessageID;
        private QRResponseData getResponse;
        private ushort getCompleted, getWarned, getFailed;

        //stuff for C-MOVE handling
        private DICOMSender moveSender;
    }
}
