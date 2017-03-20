
namespace DICOMSharp.Network.Connections
{
    internal enum PDUType
    {
        ASSOCIATE_RQ = 1,
        ASSOCIATE_AC = 2,
        ASSOCIATE_RJ = 3,
        P_DATA_TF = 4,
        RELEASE_RQ = 5,
        RELEASE_RP = 6,
        A_ABORT = 7
    }

#pragma warning disable 1591

    /// <summary>
    /// DICOM list for sources for an A-Abort
    /// </summary>
    public enum AAbortSources
    {
        DICOMULServiceUser = 0,
        Reserved1 = 1,
        DICOMULServiceProvider = 2
    }

    /// <summary>
    /// DICOM list for reasons for an A-Abort
    /// </summary>
    public enum AAbortReasons
    {
        ReasonNotSpecified = 0,
        UnrecognizedPDU = 1,
        UnexpectedPDU = 2,
        Reserved3 = 3,
        UnrecognizedPDUParameter = 4,
        UnexpectedPDUParameter = 5,
        InvalidPDUParameterValue = 6
    }

    /// <summary>
    /// List of DICOM P-DATA Command types
    /// </summary>
    public enum PDATACommands
    {
        CSTORERQ = 0x0001,
        CSTORERSP = 0x8001,

        CCANCELRQ = 0x0FFF,

        CGETRQ = 0x0010,
        CGETRSP = 0x8010,

        CFINDRQ = 0x0020,
        CFINDRSP = 0x8020,

        CMOVERQ = 0x0021,
        CMOVERSP = 0x8021,

        CECHORQ = 0x0030,
        CECHORSP = 0x8030,

        NGETRQ = 0x0110,
        NGETRSP = 0x8110,

        NACTIONRQ = 0x0130,
        NACTIONRSP = 0x8130
    }

    /// <summary>
    /// Enum for whether the data set exists or not in a P-DATA command
    /// </summary>
    public enum DataSetTypes
    {
        NoDataSet = 0x101,
        DataSetExists = 0x102
    }

    /// <summary>
    /// Statuses for P-DATA commands.  These are described in more detail in the DICOM spec in PS 3.7 Annex C (Pages 77-85), and in PS 3.4 on Page 54.
    /// </summary>
    public enum CommandStatus
    {
        Success = 0,

        Cancel = 0xFE00,

        Pending_AllOptionalKeysReturned = 0xFF00,
        Pending_SomeOptionalKeysNotReturned = 0xFF01,

        Failure_RefusedOutOfResources = 0xA700,
        Failure_IdentifierDoesntMatchSOPClass = 0xA900,

        Refused_SOPClassNotSupported = 0x0122,
        Failure_ClassInstanceConflict = 0x119,

        Warning_DuplicateSOPInstance = 0x0111,
        Warning_DuplicateInvocation = 0x210,

        Error_InvalidArgumentValue = 0x115,
        Error_InvalidAttributeValue = 0x106,
        Error_InvalidObjectInstance = 0x117,
        Error_MissingAttribute = 0x120,
        Error_MissingAttributeValue = 0x121,
        Error_MistypedArgument = 0x212,
        Error_NoSuchArgument = 0x114,
        Error_NoSuchAttribute = 0x105,
        Error_NoSuchObjectInstance = 0x112,
        Error_NoSuchSOPClass = 0x118,
        Error_ProcessingFailure = 0x110,
        Error_ResourceLimitation = 0x213,
        Error_UnrecognizedOperation = 0x211,
        Error_NoSuchActionType = 0x123
    }

    /// <summary>
    /// DICOM P-DATA command priority.
    /// </summary>
    public enum CommandPriority
    {
        Low = 2,
        Medium = 0,
        High = 1
    }

    /// <summary>
    /// List of Query/Retrieve levels for both SCP and SCU usage (for C-FIND/MOVE/GET commands)
    /// </summary>
    public enum QueryRetrieveLevel
    {
        PatientRoot,
        StudyRoot,
        PatientStudyOnly,
        ModalityWorklist,
        Unknown
    }
}
