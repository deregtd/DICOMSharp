
namespace DICOMSharp.Data.Compression
{
    /// <summary>
    /// Enumeration of all types of known DICOM-compatible compression formats.
    /// </summary>
    public enum CompressionInfo
    {
#pragma warning disable 1591
        None,
        JPEGLossless,
        JPEGLossy,
        JPEG2000,
        JPEGLSLossless,
        JPEGLSLossy,
        JPIP,
        MPEG2,
        RLE
    }
}
