
namespace DICOMSharp.Network.Connections
{
    internal enum AssociationItemType
    {
        ApplicationContext = 0x10,
        PresentationContext = 0x20,
        PresentationContextReply = 0x21,
        AbstractSyntax = 0x30,
        TransferSyntax = 0x40,
        UserInfo = 0x50,
        MaxPDULength = 0x51,
        ImplementationUID = 0x52,
        ImplementationName = 0x55
    }

#pragma warning disable 1591

    /// <summary>
    /// Associate-RJ Results type -- Documented in the DICOM Spec in PS 3.8, Page 36
    /// </summary>
    public enum AssociateRJResults
    {
        RejectedPermanent = 1,
        RejectedTransient = 2
    }

    /// <summary>
    /// Associate-RJ Source type -- Documented in the DICOM Spec in PS 3.8, Page 36
    /// </summary>
    public enum AssociateRJSources
    {
        DICOMULServiceUser = 1,
        DICOMULServiceProviderACSE = 2,
        DICOMULServiceProviderPresentation = 3
    }

    /// <summary>
    /// Associate-RJ Reasons type -- Documented in the DICOM Spec in PS 3.8, Page 36
    /// </summary>
    public enum AssociateRJReasons
    {
        //Source = 1
        NoReasonGiven = 1,
        ApplicationContextNameNotSupported = 2,
        CallingAENotRecognized = 3,
        CalledAENotRecognized = 7,

        //Source = 2
        ProtocolVersionNotSupported = 2,

        //Source = 3
        TemporaryCongestion = 1,
        LocalLimitExceeded = 2
    }
}
