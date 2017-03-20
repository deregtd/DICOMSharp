using DICOMSharp.Data.Compression;

namespace DICOMSharp.Data.Transfers
{
    /// <summary>
    /// This class represents a DICOM Transfer Syntax UID.
    /// </summary>
    public class TransferSyntax : Uid
    {
        internal TransferSyntax(string uid, string desc, bool explicitVR, bool msbSwap, CompressionInfo compression) : base(uid, desc)
        {
            ExplicitVR = explicitVR;
            MSBSwap = msbSwap;
            Compression = compression;
        }

        /// <summary>
        /// Returns a string representing the Transfer Syntax (UID and Description)
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return UidStr + " (TransSyntax: " + Desc + ")";
        }

        internal bool ExplicitVR { get; private set; }
        internal bool MSBSwap { get; private set; }

        /// <summary>
        /// Returns the <see cref="CompressionInfo"/> for the Transfer Syntax.
        /// </summary>
        public CompressionInfo Compression { get; private set; }
    }
}
