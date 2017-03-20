using System.Collections.Generic;
using DICOMSharp.Data;
using DICOMSharp.Data.Elements;
using DICOMSharp.Network.Connections;
using DICOMSharp.Data.Tags;
using System.IO;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Data.Transfers;

namespace DICOMSharp.Network.QueryRetrieve
{
    /// <summary>
    /// This structure is used for holding data from a response to a Query/Retrieve request.  This structure
    /// is used by both SCU (in which case it is filled with the response from the SCP and given to the SCU)
    /// and SCP (in which case it is filled by the SCP in response to the SCU query) operations.
    /// </summary>
    public class QRResponseData
    {
        internal QRResponseData(QueryRetrieveLevel queryLevel)
        {
            //fill response values from the DICOM request data
            QueryLevel = queryLevel;
            TagsToFill = new List<uint>();
            SPSSTags = new List<uint>();
            ResponseRows = new List<Dictionary<uint, object>>();
            FilesToSend = new Queue<SendableImage>();
        }

        /// <summary>
        /// This function is for an SCP.  It adds a set of DICOMTag to object mappings to the response structure.
        /// </summary>
        /// <param name="respRow">The mapping of DICOMTag to object data that will be sent back to the SCU.</param>
        public void AddResponseRow(Dictionary<uint, object> respRow)
        {
            ResponseRows.Add(respRow);
        }

        internal void AddResponseRow(DICOMData dataDICOM)
        {
            Dictionary<uint, object> respRow = new Dictionary<uint, object>();
            foreach (DICOMElement elem in dataDICOM.Elements.Values)
            {
                if (elem.Tag == DICOMTags.QueryRetrieveLevel)
                {
                    //ignore!
                }
                else
                    respRow[elem.Tag] = elem.Data;
            }
            ResponseRows.Add(respRow);
        }

        /// <summary>
        /// This function is for an SCP.  It will add a DICOMData object to the list of files to respond with.
        /// </summary>
        /// <param name="file"></param>
        public void AddResponseFile(DICOMData file)
        {
            FilesToSend.Enqueue(new SendableImage { DicomData = file });
        }

        /// <summary>
        /// This function is for an SCP.  It will add a file by pathname/abstract syntax/transfer syntax to the list of files to respond with.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="abstractSyntax"></param>
        /// <param name="transferSyntax"></param>
        public void AddResponseFile(string filePath, AbstractSyntax abstractSyntax, TransferSyntax transferSyntax)
        {
            FilesToSend.Enqueue(new SendableImage { FilePath = filePath, AbstractSyntax = abstractSyntax, TransferSyntax = transferSyntax });
        }

        /// <summary>
        /// This is a list of responses.  Each response is a Dictionary of DICOMTags to the data contained in
        /// that tag.  If you are an SCP filling out this structure, then you add new response rows with the
        /// <see cref="AddResponseRow(Dictionary&lt;uint, object&gt;)"/> function.  If you are an SCU getting
        /// this structure as a response to a query, your result set will be in this list.
        /// </summary>
        public List<Dictionary<uint, object>> ResponseRows { get; private set; }

        /// <summary>
        /// This contains a list of DICOM Tags that need to be filled in for each Response Row.
        /// </summary>
        public List<uint> TagsToFill { get; private set; }

        /// <summary>
        /// This is a list of all the terms being searched over by the QR request for the inside of a
        /// Scheduled Procedure Step Sequence.  If you are an SCP, then you need to look over this list
        /// and respond to anything in here in your own SPSS term.
        /// </summary>
        public List<uint> SPSSTags { get; private set; }


        internal Queue<SendableImage> FilesToSend { get; set; }

        internal QueryRetrieveLevel QueryLevel { get; private set; }
    }
}
