
namespace DICOMSharp.Network.Presentations
{
    /// <summary>
    /// An enum for the possible presentation context results
    /// </summary>
    public enum PresentationResult
    {
#pragma warning disable 1591
        Acceptance = 0,
        UserRejection = 1,
        NoReason = 2,
        AbstractSyntaxNotSupported = 3,
        TransferSyntaxesNotSupported = 4
    }
}
