using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Logging;
using DICOMSharp.Network.Connections;
using DICOMSharp.Data;

namespace DICOMSharp.Network.Workers
{
    /// <summary>
    /// This is a base class containing common code for all the SCU-type DICOM worker classes.
    /// </summary>
    public abstract class DICOMSCU
    {
        /// <summary>
        /// Initializes a new SCU base type.
        /// </summary>
        /// <param name="logger">A logger to get logging callbacks about the exchange.  May not be null.</param>
        /// <param name="scuName">Name for the SCU for logging.</param>
        /// <param name="verbose">True for incredibly verbose logging to the logger.  False for simple logging.</param>
        internal DICOMSCU(ILogger logger, string scuName, bool verbose)
        {
            this.logger = logger;

            conn = new DICOMConnection(logger, scuName, verbose);
            
            conn.AssociationAccepted += new DICOMConnection.BasicConnectionHandler(conn_AssociationAccepted);
            conn.CommandReceived += new DICOMConnection.CommandHandler(conn_CommandReceived);

            conn.AssociationRejected += new DICOMConnection.AssociationRejectedHandler(conn_AssociationRejected);
            conn.ConnectionClosed += new DICOMConnection.BasicConnectionHandler(conn_ConnectionClosed);
        }

        /// <summary>
        /// Start the SCU connection.
        /// </summary>
        /// <param name="hostAE">An entity containing the AE title to represent the SCU.</param>
        /// <param name="remoteAE">An entity containing the SCP to attempt to contact.</param>
        internal void StartConnection(ApplicationEntity hostAE, ApplicationEntity remoteAE)
        {
            wasSuccessful = true;
            conn.CallingAE = hostAE.Title;
            conn.ConnectTo(remoteAE);
        }

        /// <summary>
        /// Force-aborts the SCU operation.  This call will block until the abort is complete.
        /// </summary>
        public void Abort()
        {
            conn.CloseConnection();
        }

        /// <summary>
        /// This function must be overridden to provide actions when the connection is successfully associated.
        /// </summary>
        /// <param name="conn">The DICOMConnection in question.</param>
        protected abstract void conn_AssociationAccepted(DICOMConnection conn);
        /// <summary>
        /// This function must be overridden to provide handling of received P-DATA commands.
        /// </summary>
        /// <param name="conn">The DICOMConnection in question.</param>
        /// <param name="command">The P-DATA command ID.</param>
        /// <param name="cmdDICOM">The command DICOM set of the P-DATA command.</param>
        /// <param name="dataDICOM">The data DICOM set of the P-DATA command, if any.  Null if no data set contained in the command.</param>
        protected abstract void conn_CommandReceived(DICOMConnection conn, PDATACommands command, DICOMData cmdDICOM, DICOMData dataDICOM);


        private void conn_ConnectionClosed(DICOMConnection conn)
        {
            if (SCUFinished != null)
                SCUFinished(this, wasSuccessful && conn.Released);
        }

        private void conn_AssociationRejected(DICOMConnection conn, AssociateRJResults result, AssociateRJSources source, AssociateRJReasons reason)
        {
            wasSuccessful = false;
            logger.Log(LogLevel.Error, "Association Rejected! Result: " + result + ", Source: " + source + ", Reason: " + reason);
        }

        /// <summary>
        /// This is a handler for the <see cref="SCUFinished"/> callback.
        /// </summary>
        /// <param name="scu">A pointer to the DICOMSCU that is sending this update, in case you are using the same callback function
        /// for many simultaneous SCUs.</param>
        /// <param name="success">Returns true if the SCU successfully connected, associated, completed all its actions, and released.</param>
        public delegate void SCUFinishedHandler(DICOMSCU scu, bool success);
        /// <summary>
        /// Assign a callback function to this handler to get notification when the SCU has fully completed, one way or another.  This
        /// is called when the connection is closed.
        /// </summary>
        public event SCUFinishedHandler SCUFinished;

        /// <summary>
        /// The DICOMConnection used by the SCU.
        /// </summary>
        protected DICOMConnection conn;

        /// <summary>
        /// The DICOM Logger used by the SCU.
        /// </summary>
        protected ILogger logger;

        /// <summary>
        /// If any part of your SCU was unsuccessful, set this to false and the <see cref="SCUFinished"/> callback will return false
        /// instead of true.
        /// </summary>
        protected bool wasSuccessful;
    }
}
