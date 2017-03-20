using System.Collections.Generic;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Elements;
using System.Diagnostics;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.Abstracts;
using System;

namespace DICOMSharp.Network.QueryRetrieve
{
    /// <summary>
    /// This structure is used to hold data for a Query/Retrieve request.  For an SCU, you will fill this out to provide
    /// the data for the request, and it will be passed to the SCP, which will then respond with a <see cref="QRResponseData"/>.
    /// For an SCP, this will contain the data from a request by an SCU in a query callback, and you will use the
    /// <see cref="GenerateResponse"/> method to create a <see cref="QRResponseData"/> structure to fill out and respond with.
    /// </summary>
    public class QRRequestData
    {
        /// <summary>
        /// Create a new QRRequestData at the given query/retrieve level that will query with the given find level.
        /// </summary>
        /// <param name="queryRetrieveLevel">This is the query retrieve level for the request, which determines how the SCP
        /// will respond.</param>
        /// <param name="findLevel">This is the desired level of granularity for responses that are being requested.</param>
        public QRRequestData(QueryRetrieveLevel queryRetrieveLevel, QRLevelType findLevel)
        {
            QueryLevel = queryRetrieveLevel;
            FindLevel = findLevel;

            FillTagsList(null);

            SearchTerms = new Dictionary<uint, object>();
            foreach (uint tag in TagsToFill)
                SearchTerms[tag] = null;
        }

        internal QRRequestData(DICOMData cmd, DICOMData data)
        {
            //parse query-retrieve level
            QueryLevel = AbstractSyntaxes.Lookup((string)cmd[DICOMTags.AffectedSOPClass].Data).ParseQRLevel();

            if (QueryLevel == QueryRetrieveLevel.ModalityWorklist)
                FindLevel = QRLevelType.Study;
            else
            {
                //parse find level
                string type = ((string)data[DICOMTags.QueryRetrieveLevel].Data).Trim().ToUpper();
                if (type == "PATIENT") FindLevel = QRLevelType.Patient;
                else if (type == "STUDY") FindLevel = QRLevelType.Study;
                else if (type == "SERIES") FindLevel = QRLevelType.Series;
                else if (type == "IMAGE") FindLevel = QRLevelType.Image;
                else FindLevel = QRLevelType.Study;   //fallback
            }

            Console.Write(data.Dump());

            //pull all search terms
            SearchTerms = new Dictionary<uint, object>();
            foreach (DICOMElement elem in data.Elements.Values)
            {
                //Don't add group length elements
                if (elem.Elem == 0)
                    continue;
                if (elem.Tag == DICOMTags.QueryRetrieveLevel)
                    continue;

                if (elem.Tag == DICOMTags.ScheduledProcedureStepSequence)
                {
                    if (elem.VRShort == DICOMElementSQ.vrshort)
                    {
                        foreach (SQItem sqItem in ((DICOMElementSQ)elem).Items)
                        {
                            foreach (DICOMElement elemi in sqItem.Elements)
                            {
                                if (elemi.Elem == 0)
                                    continue;
                                if (elemi.Tag == DICOMTags.QueryRetrieveLevel)
                                    continue;

                                SearchTerms.Add(elemi.Tag, elemi.Data);
                            }
                        }
                    }
                }
                else if (elem.Data.ToString() != "")
                    SearchTerms.Add(elem.Tag, elem.Data);
            }

            FillTagsList(data);
        }

        private void FillTagsList(DICOMData data)
        {
            //get the tags needed to fill
            TagsToFill = new List<uint>();
            SPSSTags = new List<uint>();

            //In the spec, PS 9.4, Pages 77-79
            switch (FindLevel)
            {
                case QRLevelType.Patient:
                    //Required
                    TagsToFill.Add(DICOMTags.PatientName);
                    TagsToFill.Add(DICOMTags.PatientID);
                    break;

                case QRLevelType.Study:
                    //Required Study-level
                    TagsToFill.Add(DICOMTags.StudyDate);
                    TagsToFill.Add(DICOMTags.StudyTime);
                    TagsToFill.Add(DICOMTags.AccessionNumber);
                    TagsToFill.Add(DICOMTags.StudyID);
                    TagsToFill.Add(DICOMTags.StudyInstanceUID);
                    break;

                case QRLevelType.Series:
                    //Required series-level
                    TagsToFill.Add(DICOMTags.Modality);
                    TagsToFill.Add(DICOMTags.SeriesNumber);
                    TagsToFill.Add(DICOMTags.SeriesInstanceUID);
                    break;

                case QRLevelType.Image:
                    //Required image-level
                    TagsToFill.Add(DICOMTags.InstanceNumber);
                    TagsToFill.Add(DICOMTags.SOPInstanceUID);
                    break;
            }

            if (data != null)
            {
                //fill anything else in we don't already have from required
                foreach (DICOMElement elem in data.Elements.Values)
                {
                    if (elem.Elem == 0)
                        continue;
                    if (elem.Tag == DICOMTags.QueryRetrieveLevel)
                        continue;

                    if (elem.Tag == DICOMTags.ScheduledProcedureStepSequence)
                    {
                        if (elem.VRShort == DICOMElementSQ.vrshort)
                        {
                            foreach (SQItem sqItem in ((DICOMElementSQ)elem).Items)
                            {
                                foreach (DICOMElement elemi in sqItem.Elements)
                                {
                                    if (elemi.Elem == 0)
                                        continue;
                                    if (elemi.Tag == DICOMTags.QueryRetrieveLevel)
                                        continue;

                                    SPSSTags.Add(elemi.Tag);
                                }
                            }
                        }
                    }
                    else
                        TagsToFill.Add(elem.Tag);
                }
            }
        }

        internal DICOMData CreateSearchData()
        {
            DICOMData data = new DICOMData();

            if (FindLevel == QRLevelType.Patient) data[DICOMTags.QueryRetrieveLevel].Data = "PATIENT";
            else if (FindLevel == QRLevelType.Study) data[DICOMTags.QueryRetrieveLevel].Data = "STUDY";
            else if (FindLevel == QRLevelType.Series) data[DICOMTags.QueryRetrieveLevel].Data = "SERIES";
            else if (FindLevel == QRLevelType.Image) data[DICOMTags.QueryRetrieveLevel].Data = "IMAGE";
            else data[DICOMTags.QueryRetrieveLevel].Data = "STUDY"; //fallback...

            foreach (uint tag in SearchTerms.Keys)
                data[tag].Data = SearchTerms[tag];
            return data;
        }

        /// <summary>
        /// When your SCP has received a QRRequestData, you must respond with a <see cref="QRResponseData"/>.
        /// Use this method to generate a response structure.  The response structure will already have its
        /// <see cref="QRResponseData.TagsToFill"/> list filled out, so you can just fill in all of those
        /// for the response rows.
        /// </summary>
        /// <returns>A new QRResponse data to fill with response rows.</returns>
        public QRResponseData GenerateResponse()
        {
            QRResponseData response = new QRResponseData(QueryLevel);

            response.TagsToFill.AddRange(TagsToFill);
            response.SPSSTags.AddRange(SPSSTags);

            return response;
        }


        /// <summary>
        /// The find level determines the level of granularity for objects being returned by the query.
        /// </summary>
        public QRLevelType FindLevel { get; private set; }

        /// <summary>
        /// This is the DICOM query/retrieve level, which determines the behavior of exactly how the
        /// SCP should respond to a query.
        /// </summary>
        public QueryRetrieveLevel QueryLevel { get; private set; }

        /// <summary>
        /// This is a lookup of all the terms being searched over by the QR request.  If you are an SCP,
        /// then limit your responses to those matching these search terms.  If you are an SCU creating
        /// a query, fill this out with search terms to either limit your query or show what fields you
        /// want in return.  If this is a MWL query, this will also contain values from the SPSSTags
        /// list.
        /// </summary>
        public Dictionary<uint, object> SearchTerms { get; private set; }

        /// <summary>
        /// If you are creating a request as an SCU, this is automatically filled with the minimum required
        /// tags to fill out for your search terms.  If you are an SCP receiving this request, then this
        /// will be used to fill out the response in the GenerateResponse method.
        /// </summary>
        public List<uint> TagsToFill { get; private set; }

        /// <summary>
        /// This is a list of all the terms being searched over by the QR request for the inside of a
        /// Scheduled Procedure Step Sequence.  If you are an SCP, then you need to look over this list
        /// and respond to anything in here in your own SPSS term.
        /// </summary>
        public List<uint> SPSSTags { get; private set; }
    }
}
