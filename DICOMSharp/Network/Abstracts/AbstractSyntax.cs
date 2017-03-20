using DICOMSharp.Data;
using DICOMSharp.Network.Connections;

namespace DICOMSharp.Network.Abstracts
{
    /// <summary>
    /// This class represents a DICOM Abstract Syntax UID.
    /// </summary>
    public class AbstractSyntax : Uid
    {
        internal AbstractSyntax(string uid, string desc)
            : base(uid, desc)
        {
        }

        /// <summary>
        /// Returns a string representing the Abstract Syntax (UID and Description)
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return UidStr + " (AbsSyntax: " + Desc + ")";
        }

        internal QueryRetrieveLevel ParseQRLevel()
        {
            if (this == AbstractSyntaxes.PatientRootQueryRetrieveInformationModelFIND) return QueryRetrieveLevel.PatientRoot;
            if (this == AbstractSyntaxes.PatientRootQueryRetrieveInformationModelMOVE) return QueryRetrieveLevel.PatientRoot;
            if (this == AbstractSyntaxes.PatientRootQueryRetrieveInformationModelGET) return QueryRetrieveLevel.PatientRoot;

            if (this == AbstractSyntaxes.StudyRootQueryRetrieveInformationModelFIND) return QueryRetrieveLevel.StudyRoot;
            if (this == AbstractSyntaxes.StudyRootQueryRetrieveInformationModelMOVE) return QueryRetrieveLevel.StudyRoot;
            if (this == AbstractSyntaxes.StudyRootQueryRetrieveInformationModelGET) return QueryRetrieveLevel.StudyRoot;

            if (this == AbstractSyntaxes.PatientStudyOnlyQueryRetrieveInformationModelFINDRetired) return QueryRetrieveLevel.PatientStudyOnly;
            if (this == AbstractSyntaxes.PatientStudyOnlyQueryRetrieveInformationModelMOVERetired) return QueryRetrieveLevel.PatientStudyOnly;
            if (this == AbstractSyntaxes.PatientStudyOnlyQueryRetrieveInformationModelGETRetired) return QueryRetrieveLevel.PatientStudyOnly;

            if (this == AbstractSyntaxes.ModalityWorklistInformationModelFIND) return QueryRetrieveLevel.ModalityWorklist;

            return QueryRetrieveLevel.Unknown;
        }
    }
}
