using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DICOMSharp.Network.Connections
{
    /// <summary>
    /// This class represents a DICOM Application Entity, and is used throughout DICOMSharp whenever an entity
    /// is represented, even if only some fields are needed.
    /// 
    /// The class provides a Note field in case you'd like to attach any custom information to your Entity in
    /// your application.
    /// </summary>
    public class ApplicationEntity
    {
        /// <summary>
        /// Private empty constructor for JSON deserialization only.
        /// </summary>
        protected ApplicationEntity()
        {
        }

        /// <summary>
        /// Creates an entity with only the AE Title filled in.  This is used commonly for SCU-based workers
        /// like <see cref="DICOMSharp.Network.Workers.DICOMEchoer"/>.
        /// </summary>
        /// <param name="title"><see cref="Title"/></param>
        public ApplicationEntity(string title)
        {
            Title = title;
            Address = null;
            Port = 0;
        }

        /// <summary>
        /// Creates an entity with both the AE title and port filled in, but no address.  This is used commonly
        /// for SCP-based workers like <see cref="DICOMSharp.Network.Workers.DICOMListener"/>.
        /// </summary>
        /// <param name="title"><see cref="Title"/></param>
        /// <param name="port"><see cref="Port"/></param>
        public ApplicationEntity(string title, ushort port)
            : this(title)
        {
            Port = port;
        }

        /// <summary>
        /// Creates an entity with everything needed to create an SCU connection.  This is used commonly for
        /// the SCU-based workers like <see cref="DICOMSharp.Network.Workers.DICOMFinder"/>.
        /// </summary>
        /// <param name="title"><see cref="Title"/></param>
        /// <param name="address"><see cref="Port"/></param>
        /// <param name="port"><see cref="Port"/></param>
        public ApplicationEntity(string title, string address, ushort port)
            : this(title, port)
        {
            Address = address;
        }

        /// <summary>
        /// The AE title -- no longer than 16 characters.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The network address (host or IP) of the entity.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// A TCP port for the entity.
        /// </summary>
        public ushort Port { get; set; }
    }
}
