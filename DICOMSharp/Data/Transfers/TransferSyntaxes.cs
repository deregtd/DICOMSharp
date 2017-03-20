using System.Collections.Generic;
using System.Reflection;

using DICOMSharp.Data.Compression;

namespace DICOMSharp.Data.Transfers
{
    /// <summary>
    /// Public enum (basically) of transfer syntaxes available for use and supported by DICOMSharp.
    /// These are outlined in a little more detail in the DICOM spec in PS 3.6, pages 120+
    /// </summary>
    public static class TransferSyntaxes
    {
        static TransferSyntaxes()
        {
            //Use reflection to make a lookup array for the transfer syntaxes
            syntaxLookup = new Dictionary<string, TransferSyntax>();

            foreach (FieldInfo field in typeof(TransferSyntaxes).GetFields())
            {
                object val = field.GetValue(null);
                if (val is TransferSyntax)
                {
                    TransferSyntax ts = (TransferSyntax)val;
                    syntaxLookup[ts.UidStr] = ts;
                    Uid.MasterLookup[ts.UidStr] = ts;
                }
            }
        }

        private static Dictionary<string, TransferSyntax> syntaxLookup;

        /// <summary>
        /// Looks up a transfer syntax detail object by UID
        /// </summary>
        /// <param name="uid">The Transfer syntax UID to look up</param>
        /// <returns>A TransferSyntax object from the dictionary with more info about the specified syntax, if available, otherwise a generic "unknown" syntax.</returns>
        public static TransferSyntax Lookup(string uid)
        {
            if (syntaxLookup.ContainsKey(uid))
                return syntaxLookup[uid];
            return new TransferSyntax(uid, "Unknown: " + uid, false, false, CompressionInfo.None);    //last ditch effort?
        }

        internal static void Init() { }

#pragma warning disable 1591
        public static TransferSyntax ImplicitVRLittleEndian = new TransferSyntax("1.2.840.10008.1.2", "Implicit VR Little Endian: Default Transfer Syntax for DICOM", false, false, CompressionInfo.None);

        public static TransferSyntax ExplicitVRLittleEndian = new TransferSyntax("1.2.840.10008.1.2.1", "Explicit VR Little Endian", true, false, CompressionInfo.None);
        public static TransferSyntax DeflatedExplicitVRLittleEndian = new TransferSyntax("1.2.840.10008.1.2.1.99", "Deflated Explicit VR Little Endian", true, false, CompressionInfo.None);
        public static TransferSyntax ExplicitVRBigEndian = new TransferSyntax("1.2.840.10008.1.2.2", "Explicit VR Big Endian", true, true, CompressionInfo.None);

        public static TransferSyntax JPEGBaselineProcess1 = new TransferSyntax("1.2.840.10008.1.2.4.50", "JPEG Baseline (Process 1): Default Transfer Syntax for Lossy JPEG 8 Bit Image Compression", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGExtendedProcess24 = new TransferSyntax("1.2.840.10008.1.2.4.51", "JPEG Extended (Process 2 & 4): Default Transfer Syntax for Lossy JPEG 12 Bit Image Compression (Process 4 only)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGExtendedProcess35Retired = new TransferSyntax("1.2.840.10008.1.2.4.52", "JPEG Extended (Process 3 & 5) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGSpectralSelectionNonHierarchicalProcess68Retired = new TransferSyntax("1.2.840.10008.1.2.4.53", "JPEG Spectral Selection, Non-Hierarchical (Process 6 & 8) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGSpectralSelectionNonHierarchicalProcess79Retired = new TransferSyntax("1.2.840.10008.1.2.4.54", "JPEG Spectral Selection, Non-Hierarchical (Process 7 & 9) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGFullProgressionNonHierarchicalProcess1012Retired = new TransferSyntax("1.2.840.10008.1.2.4.55", "JPEG Full Progression, Non-Hierarchical (Process 10 & 12) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGFullProgressionNonHierarchicalProcess1113Retired = new TransferSyntax("1.2.840.10008.1.2.4.56", "JPEG Full Progression, Non-Hierarchical (Process 11 & 13) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGLosslessNonHierarchicalProcess14 = new TransferSyntax("1.2.840.10008.1.2.4.57", "JPEG Lossless, Non-Hierarchical (Process 14)", true, false, CompressionInfo.JPEGLossless);
        public static TransferSyntax JPEGLosslessNonHierarchicalProcess15Retired = new TransferSyntax("1.2.840.10008.1.2.4.58", "JPEG Lossless, Non-Hierarchical (Process 15) (Retired)", true, false, CompressionInfo.JPEGLossless);
        public static TransferSyntax JPEGExtendedHierarchicalProcess1618Retired = new TransferSyntax("1.2.840.10008.1.2.4.59", "JPEG Extended, Hierarchical (Process 16 & 18) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGExtendedHierarchicalProcess1719Retired = new TransferSyntax("1.2.840.10008.1.2.4.60", "JPEG Extended, Hierarchical (Process 17 & 19) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGSpectralSelectionHierarchicalProcess2022Retired = new TransferSyntax("1.2.840.10008.1.2.4.61", "JPEG Spectral Selection, Hierarchical (Process 20 & 22) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGSpectralSelectionHierarchicalProcess2123Retired = new TransferSyntax("1.2.840.10008.1.2.4.62", "JPEG Spectral Selection, Hierarchical (Process 21 & 23) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGFullProgressionHierarchicalProcess2426Retired = new TransferSyntax("1.2.840.10008.1.2.4.63", "JPEG Full Progression, Hierarchical (Process 24 & 26) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGFullProgressionHierarchicalProcess2527Retired = new TransferSyntax("1.2.840.10008.1.2.4.64", "JPEG Full Progression, Hierarchical (Process 25 & 27) (Retired)", true, false, CompressionInfo.JPEGLossy);
        public static TransferSyntax JPEGLosslessHierarchicalProcess28Retired = new TransferSyntax("1.2.840.10008.1.2.4.65", "JPEG Lossless, Hierarchical (Process 28) (Retired)", true, false, CompressionInfo.JPEGLossless);
        public static TransferSyntax JPEGLosslessHierarchicalProcess29Retired = new TransferSyntax("1.2.840.10008.1.2.4.66", "JPEG Lossless, Hierarchical (Process 29) (Retired)", true, false, CompressionInfo.JPEGLossless);
        public static TransferSyntax JPEGLosslessNonHierarchicalFirstOrderPredictionProcess14 = new TransferSyntax("1.2.840.10008.1.2.4.70", "JPEG Lossless, Non-Hierarchical, First-Order Prediction (Process 14 [Selection Value 1]): Default Transfer Syntax for Lossless JPEG Image Compression", true, false, CompressionInfo.JPEGLossless);

        public static TransferSyntax JPEGLSLosslessImageCompression = new TransferSyntax("1.2.840.10008.1.2.4.80", "JPEG-LS Lossless Image Compression", true, false, CompressionInfo.JPEGLSLossless);
        public static TransferSyntax JPEGLSLossyNearLosslessImageCompression = new TransferSyntax("1.2.840.10008.1.2.4.81", "JPEG-LS Lossy (Near-Lossless) Image Compression", true, false, CompressionInfo.JPEGLSLossy);

        public static TransferSyntax JPEG2000ImageCompressionLosslessOnly = new TransferSyntax("1.2.840.10008.1.2.4.90", "JPEG 2000 Image Compression (Lossless Only)", true, false, CompressionInfo.JPEG2000);
        public static TransferSyntax JPEG2000ImageCompression = new TransferSyntax("1.2.840.10008.1.2.4.91", "JPEG 2000 Image Compression", true, false, CompressionInfo.JPEG2000);
        public static TransferSyntax JPEG2000Part2MulticomponentImageCompressionLosslessOnly = new TransferSyntax("1.2.840.10008.1.2.4.92", "JPEG 2000 Part 2 Multi-component Image Compression (Lossless Only)", true, false, CompressionInfo.JPEG2000);
        public static TransferSyntax JPEG2000Part2MulticomponentImageCompression = new TransferSyntax("1.2.840.10008.1.2.4.93", "JPEG 2000 Part 2 Multi-component Image Compression", true, false, CompressionInfo.JPEG2000);

        public static TransferSyntax JPIPReferenced = new TransferSyntax("1.2.840.10008.1.2.4.94", "JPIP Referenced", true, false, CompressionInfo.JPIP);
        public static TransferSyntax JPIPReferencedDeflate = new TransferSyntax("1.2.840.10008.1.2.4.95", "JPIP Referenced Deflate", true, false, CompressionInfo.JPIP);

        public static TransferSyntax MPEG2MainProfileMainLevel = new TransferSyntax("1.2.840.10008.1.2.4.100", "MPEG2 Main Profile @ Main Level", true, false, CompressionInfo.MPEG2);
        public static TransferSyntax MPEG2MainProfileHighLevel = new TransferSyntax("1.2.840.10008.1.2.4.101", "MPEG2 Main Profile @ High Level", true, false, CompressionInfo.MPEG2);

        public static TransferSyntax RLELossless = new TransferSyntax("1.2.840.10008.1.2.5", "RLE Lossless", true, false, CompressionInfo.RLE);

        public static TransferSyntax RFC2557MIMEencapsulation = new TransferSyntax("1.2.840.10008.1.2.6.1", "RFC 2557 MIME encapsulation", true, false, CompressionInfo.None);
        public static TransferSyntax XMLEncoding = new TransferSyntax("1.2.840.10008.1.2.6.2", "XML Encoding", true, false, CompressionInfo.None);

        //Preferred support order
        public static List<TransferSyntax> preferredSyntaxOrder = new List<TransferSyntax>(new TransferSyntax[] {
            ImplicitVRLittleEndian,
            ExplicitVRLittleEndian,
            DeflatedExplicitVRLittleEndian,

            JPEGBaselineProcess1,
            JPEGExtendedProcess24,
            JPEGExtendedProcess35Retired,
            JPEGSpectralSelectionNonHierarchicalProcess68Retired,
            JPEGSpectralSelectionNonHierarchicalProcess79Retired,
            JPEGFullProgressionNonHierarchicalProcess1012Retired,
            JPEGFullProgressionNonHierarchicalProcess1113Retired,
            JPEGLosslessNonHierarchicalProcess14,
            JPEGLosslessNonHierarchicalProcess15Retired,
            JPEGExtendedHierarchicalProcess1618Retired,
            JPEGExtendedHierarchicalProcess1719Retired,
            JPEGSpectralSelectionHierarchicalProcess2022Retired,
            JPEGSpectralSelectionHierarchicalProcess2123Retired,
            JPEGFullProgressionHierarchicalProcess2426Retired,
            JPEGFullProgressionHierarchicalProcess2527Retired,
            JPEGLosslessHierarchicalProcess28Retired,
            JPEGLosslessHierarchicalProcess29Retired,
            JPEGLosslessNonHierarchicalFirstOrderPredictionProcess14,
            JPEGLSLosslessImageCompression,
            JPEGLSLossyNearLosslessImageCompression,
            JPEG2000ImageCompressionLosslessOnly,
            JPEG2000ImageCompression,
            JPEG2000Part2MulticomponentImageCompressionLosslessOnly,
            JPEG2000Part2MulticomponentImageCompression,
            JPIPReferenced,
            JPIPReferencedDeflate,
            MPEG2MainProfileMainLevel,
            MPEG2MainProfileHighLevel,
            RLELossless,

            ExplicitVRBigEndian
        });

        //Unsupported list
        public static HashSet<TransferSyntax> unsupportedSyntaxes = new HashSet<TransferSyntax>(new TransferSyntax[] {
            XMLEncoding,
            RFC2557MIMEencapsulation
        });

    }
}
