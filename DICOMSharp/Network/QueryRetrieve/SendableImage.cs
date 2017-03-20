using DICOMSharp.Data;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Network.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICOMSharp.Network.QueryRetrieve
{
    /// <summary>
    /// A simple structure to hold info about an image for sending.  You can either use a dicom data or a file path + transfer syntax + abstract syntax set.
    /// </summary>
    public struct SendableImage
    {
        /// <summary>
        /// To just send a DICOMData directly that's already in memory, set it here.
        /// </summary>
        public DICOMData DicomData;

        /// <summary>
        /// The file path to the DICOM file to parse and load.
        /// </summary>
        public string FilePath;
        /// <summary>
        /// The transfer syntax of this DICOM file (needs to be prepopulated.)
        /// </summary>
        public TransferSyntax TransferSyntax;
        /// <summary>
        /// The abstract syntax of this DICOM file (needs to be prepopulated.)
        /// </summary>
        public AbstractSyntax AbstractSyntax;
    }
}
