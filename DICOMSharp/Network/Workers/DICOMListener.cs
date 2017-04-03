using System.Net.Sockets;
using System.Net;
using DICOMSharp.Network.Connections;
using DICOMSharp.Logging;
using DICOMSharp.Data;
using DICOMSharp.Network.Presentations;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Network.Abstracts;
using System.Threading;
using DICOMSharp.Network.QueryRetrieve;
using System.Collections.Generic;
using System.ComponentModel;
using DICOMSharp.Util;
using DICOMSharp.Data.Tags;

namespace DICOMSharp.Network.Workers
{
    /// <summary>
    /// This class is a helper class representing a DICOM SCP and handles the winsock listening on a port.  It can either
    /// automatically handle SCP actions (like receiving images) or provides event callbacks for a developer to receive
    /// information about the provided services.
    /// </summary>
    public class DICOMListener
    {
        /// <summary>
        /// Initialize a new listener.  You must provide a logging mechanism and a verbosity level.
        /// </summary>
        /// <param name="logger">A DICOM logger to get information about the listener.  May not be null.</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        public DICOMListener(ILogger logger, bool verbose)
        {
            this.logger = logger;
            this.VerboseLogging = verbose;
            this._connections = new List<DICOMConnection>();
            this._connectionCounter = 1;
        }

        /// <summary>
        /// Tell the listener to begin listening for DICOM connections on a given port.
        /// </summary>
        /// <param name="port">What TCP port to listen for DICOM connections on?</param>
        /// <returns>True for listening successfully starting.  False if something went wrong (usually port busy).</returns>
        public void StartListening(ushort port)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            listenSocket.Listen(16);    //16? why not!

            listenThread = new Thread(new ThreadStart(ListenThread));
            listenThread.Start();
        }

        /// <summary>
        /// Stops the listening thread non-gracefully.  This call blocks until the thread quits, which is
        /// usually quickly/almost immediately.
        /// </summary>
        public void StopListening()
        {
            if (listenSocket != null)
                listenSocket.Close();

            if (listenThread != null)
            {
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;
            }
        }

        private void ListenThread()
        {
            try
            {
                logger.Log(LogLevel.Info, "Listen Thread Started");
                while (true)
                {
                    Socket remoteSock = listenSocket.Accept();
                    if (remoteSock != null)
                    {
                        DICOMConnection conn = new DICOMConnection(logger, this._connectionCounter.ToString(), VerboseLogging);

                        conn.AssociationRequested += new DICOMConnection.BasicConnectionHandler(conn_AssociationRequested);
                        conn.CommandReceived += new DICOMConnection.CommandHandler(conn_CommandReceived);
                        conn.ConnectionClosed += new DICOMConnection.BasicConnectionHandler(conn_ConnectionClosed);

                        conn.SupportedAbstractSyntaxes.Add(AbstractSyntaxes.VerificationSOPClass);
                        foreach (AbstractSyntax syntax in AbstractSyntaxes.StorageSyntaxes)
                            conn.SupportedAbstractSyntaxes.Add(syntax);
                        foreach (AbstractSyntax syntax in AbstractSyntaxes.QueryRetrieveSyntaxes)
                            conn.SupportedAbstractSyntaxes.Add(syntax);

                        conn.HandleSocket(remoteSock);

                        this._connections.Add(conn);
                        this._connectionCounter++;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                logger.Log(LogLevel.Info, "Listen Thread Stopped");
            }
            catch (SocketException)
            {
                logger.Log(LogLevel.Info, "Listen Thread Stopped");
            }
        }

        private void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM)
        {
            switch (command)
            {
                case PDATACommands.CECHORQ:
                    conn.LogLine(LogLevel.Info, "Received C-ECHO-RQ");
                    conn.SendCECHORSP();
                    break;
                case PDATACommands.CSTORERQ:
                    conn.LogLine(LogLevel.Info, "Received C-STORE-RQ");

                    //Add source AE title to image
                    dataDICOM[DICOMTags.SourceApplicationEntityTitle].Data = conn.CallingAE;

                    if (StoreRequest != null)
                        StoreRequest(conn, dataDICOM);
                    else
                        conn.SendCSTORERSP(CommandStatus.Success);
                    break;
                case PDATACommands.CSTORERSP:
                    conn.LogLine(LogLevel.Info, "Received C-STORE-RSP");

                    break;
                case PDATACommands.CFINDRQ:
                    conn.LogLine(LogLevel.Info, "Received C-FIND-RQ");
                    if (FindRequest != null)
                    {
                        QRResponseData response = FindRequest(conn, new QRRequestData(cmdDICOM, dataDICOM));
                        conn.SendCFINDRSP(response);
                    }
                    else
                        conn.SendCFINDRSP(null, CommandStatus.Success);
                    break;
                case PDATACommands.CGETRQ:
                    conn.LogLine(LogLevel.Info, "Received C-GET-RQ");
                    if (GetRequest != null)
                    {
                        QRResponseData response = GetRequest(conn, new QRRequestData(cmdDICOM, dataDICOM));
                        conn.StartGetResponse(response);
                    }
                    else
                        conn.SendCGETRSP((ushort)cmdDICOM[DICOMTags.MessageID].Data, CommandStatus.Success, 0, 0, 0, 0);
                    break;
                case PDATACommands.CMOVERQ:
                    conn.LogLine(LogLevel.Info, "Received C-MOVE-RQ");
                    if (MoveRequest != null)
                    {
                        string newAE = cmdDICOM[DICOMTags.MoveDestination].Display.Trim();
                        ApplicationEntity entity = null;
                        if (EntityLookup != null)
                            entity = EntityLookup(newAE);

                        if (entity != null)
                        {
                            QRResponseData response = MoveRequest(conn, new QRRequestData(cmdDICOM, dataDICOM));
                            conn.StartMoveResponse(entity, response);
                        }
                        else
                        {
                            conn.LogLine(LogLevel.Warning, "No entity found for the MOVE request: " + newAE);
                            conn.SendCMOVERSP(CommandStatus.Error_ProcessingFailure, 0, 0, 0, 0);
                        }
                    }
                    else
                        conn.SendCMOVERSP(CommandStatus.Error_ProcessingFailure, 0, 0, 0, 0);
                    break;
                case PDATACommands.NGETRQ:
                    conn.LogLine(LogLevel.Info, "Received N-GET-RQ");

                    // No idea what we're supposed to do with these yet
                    conn.SendNGETRSP(CommandStatus.Refused_SOPClassNotSupported, null);
                    break;
                default:
                    conn.LogLine(LogLevel.Warning, "Unhandled P-DATA Command Type: " + command + "...");
                    break;
            }
        }

        private void conn_AssociationRequested(DICOMConnection conn)
        {
            if (VerboseLogging)
            {
                foreach (PresentationContext context in conn.PresentationContexts.Values)
                {
                    conn.LogLine(LogLevel.Debug, "Presentation Context: " + context.ContextID + ": " + context.AbstractSyntaxSpecified.UidStr);
                    foreach (TransferSyntax syntax in context.TransferSyntaxesProposed)
                        conn.LogLine(LogLevel.Debug, "* Transfer Syntax: " + syntax.UidStr);
                }
            }

            if (AssociationRequest != null)
            {
                AssociationRequest(conn);
            }
            else
            {
                //No handler set up, so go promiscuous!
                conn.SendAssociateAC();
            }
        }

        void conn_ConnectionClosed(DICOMConnection conn)
        {
            if (ConnectionClosed != null)
                ConnectionClosed(conn);

            this._connections.Remove(conn);
        }

        /// <summary>
        /// Handler function prototype for callbacks for <see cref="FindRequest"/>, <see cref="GetRequest"/>, and <see cref="MoveRequest"/>.
        /// </summary>
        /// <param name="conn">The <see cref="DICOMConnection"/> that the request was received on</param>
        /// <param name="request">The <see cref="QRRequestData"/> that contains the parsed data request</param>
        /// <returns>You must return a <see cref="QRResponseData"/> that contains the data to respond with.</returns>
        public delegate QRResponseData QRRequestHandler(DICOMConnection conn, QRRequestData request);

        /// <summary>
        /// Event callback for C-FIND requests.  If you add a callback to this, you must then send a response, since the DICOMListener will not.
        /// The easiest way to do this is to fill out a <see cref="QRResponseData"/> structure and send them to
        /// <see cref="DICOMSharp.Network.Connections.DICOMConnection.SendCFINDRSP(QRResponseData)"/>.
        /// If this callback is not used, the default callback successfully returns 0 results.
        /// </summary>
        public event QRRequestHandler FindRequest;
        /// <summary>
        /// Event callback for C-GET requests.  From in your handler, you must call <see cref="DICOMConnection.SendCGETRSP"/> with any C-GET responses
        /// that you are providing.  The default callback returns 0 results.
        /// </summary>
        public event QRRequestHandler GetRequest;
        /// <summary>
        /// Event callback for C-MOVE requests.  From in your handler, you must call <see cref="DICOMConnection.StartMoveResponse"/> with any
        /// C-MOVE responses that you are providing.  The default callback returns 0 results.
        /// </summary>
        public event QRRequestHandler MoveRequest;


        /// <summary>
        /// Handler function prototype for callbacks for <see cref="StoreRequest"/>.
        /// </summary>
        /// <param name="conn">The <see cref="DICOMConnection"/> that the request was received on</param>
        /// <param name="data">The <see cref="DICOMData"/> that contains the request's data packet (contains the search parameters)</param>
        public delegate void StoreRequestHandler(DICOMConnection conn, DICOMData data);
        /// <summary>
        /// Event callback for C-STORE requests.  Any data provided to the SCP in a C-STORE-RQ will be passed to this event handler.
        /// If this event callback is not set, then C-STORE-RSP is automatically sent.  If this callback is set, then you must send
        /// <see cref="DICOMConnection.SendCSTORERSP"/> to accept or reject the STORE-RQ -- DICOMListener will not provide an automatic
        /// response of its own.
        /// </summary>
        public event StoreRequestHandler StoreRequest;

        /// <summary>
        /// A basic handler function prototype for callbacks for <see cref="AssociationRequest"/> and <see cref="ConnectionClosed"/>.
        /// </summary>
        /// <param name="conn">The <see cref="DICOMConnection"/> that the request was received on</param>
        public delegate void BasicConnectionHandler(DICOMConnection conn);
        /// <summary>
        /// Event callback for DICOM Association requests.  By default, the DICOMListener is promiscuous (accepts all connections that
        /// match the correct calling AE title).  If this event callback is set, then the event callback must send
        /// <see cref="DICOMConnection.SendAssociateAC"/> to accept or <see cref="DICOMConnection.SendAssociateRJ"/> to reject the
        /// association -- DICOMListener will then not provide an automatic response of any sort.
        /// </summary>
        public event BasicConnectionHandler AssociationRequest;
        /// <summary>
        /// Event callback for when any connection handled by the listener is closed, for any reason.  This should be the last
        /// notification you get about any open connection.
        /// </summary>
        public event BasicConnectionHandler ConnectionClosed;

        /// <summary>
        /// A callback handler for <see cref="EntityLookup"/>.
        /// </summary>
        /// <param name="aeTitle">The AE title that DICOMListener needs more information about.</param>
        /// <returns>Please return a filled out ApplicationEntity structure containing an address and port for the AE title asked about.</returns>
        public delegate ApplicationEntity EntityLookupHandler(string aeTitle);
        /// <summary>
        /// If the listener will be handling C-MOVE requests, it needs to be able to look up full entity information (including
        /// address/port) from an AE title alone.  Hence, please fill in this callback.  If the callback returns null or there is
        /// no callback specified, any move request will be denied.
        /// </summary>
        public event EntityLookupHandler EntityLookup;

        /// <summary>
        /// Gets or sets whether to use verbose logging for future connections handled by the DICOMListener.  Changing this
        /// will not affect any already open DICOMConnections.
        /// </summary>
        public bool VerboseLogging;

        /// <summary>
        /// Returns if the listening thread is currently running.
        /// </summary>
        public bool IsListening { get { return listenThread != null && listenThread.IsAlive; } }

        private Thread listenThread;
        private List<DICOMConnection> _connections;
        private int _connectionCounter;

        private ILogger logger;

        private Socket listenSocket;
    }
}
