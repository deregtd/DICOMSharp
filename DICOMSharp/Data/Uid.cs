using System.Collections.Generic;
using DICOMSharp.Util;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Data.Transfers;
using System.Text;

namespace DICOMSharp.Data
{
    /// <summary>
    /// This class represents a DICOM UID.  These are used for Transfer Syntaxes, Abstract Syntaxes, and many other places throughout the DICOM spec (any UI-typed field).
    /// </summary>
    public class Uid
    {
        internal Uid(string uid) : this(uid, uid) { }

        internal Uid(string uid, string desc)
        {
            this.UidStr = uid;
            this.Desc = desc;
        }

        /// <summary>
        /// Convert a UID into a DICOM-valid byte array (0-64 chars, odd lengths terminated in an extra 0, etc.)
        /// </summary>
        /// <returns>The byte string, properly null terminated as needed</returns>
        public static byte[] UidToBytes(string uidRaw)
        {
            // UID encoding rules: PS 3.5, Pages 61-62
            var uidStr = SanitizeUid(uidRaw);
            if ((uidStr.Length & 1) > 0)
            {
                // Zero-pad odd-lengthed UIDs
                uidStr += '\0';
            }
            return Encoding.ASCII.GetBytes(uidStr);
        }

        /// <summary>
        /// Ensure that UIDs pass basic checks (can't be more than 64 chars long, can't end with a period).
        /// </summary>
        /// <param name="uidRaw">UID to sanitize</param>
        /// <returns>Sanitized UID</returns>
        public static string SanitizeUid(string uidRaw)
        {
            var uidStr = uidRaw.Replace("\0", "").Trim();
            if (uidStr.Length > 64)
            {
                uidStr = uidStr.Substring(0, 64);
            }
            if (uidStr.Length > 0 && uidStr[uidStr.Length - 1] == '.')
            {
                uidStr = uidStr.Substring(0, uidStr.Length - 1);
            }
            return uidStr;
        }

        /// <summary>
        /// Convert a raw byte array for a Uid into a string without trailing zeroes or padding.
        /// </summary>
        /// <param name="rawUid">The incoming raw bytes</param>
        /// <returns>The properly formatted string ready for comparison</returns>
        public static string UidRawToString(byte[] rawUid)
        {
            if (rawUid.Length == 0)
            {
                return string.Empty;
            }

            // Pull off the trailing zero before returning
            return Encoding.ASCII.GetString(rawUid, 0, rawUid[rawUid.Length - 1] == 0 ? rawUid.Length - 1 : rawUid.Length).Trim();
        }

        /// <summary>
        /// Return this Uid's contents as a DICOM-value encoded UID
        /// </summary>
        /// <returns>The Uid as a byte array</returns>
        public byte[] ToBytes()
        {
            return Uid.UidToBytes(this.UidStr);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this Uid.
        /// </summary>
        /// <returns>
        /// If the Uid is a known (in the DICOM Spec) UID, then this will return the name of the UID with the raw UID (i.e. "1.2.840.10008.1.2 (Implicit VR Little Endian: Default Transfer Syntax for DICOM)".)
        /// If the Uid is a private UID, then it will simply return the UID as a string.
        /// </returns>
        public override string ToString()
        {
            if (UidStr == Desc)
                return UidStr;

            return UidStr + " (" + Desc + ")";
        }

        /// <summary>
        /// Handles equality comparisons with other Uid objects based on the Uid string content of the Uid.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var objUid = obj as Uid;
            if (objUid == null)
            {
                return false;
            }
            return objUid.UidStr == UidStr;
        }

        /// <summary>
        /// Overrides GetHashCode to compare against UidStr for dictionaries.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return UidStr.GetHashCode();
        }

        /// <summary>
        /// Returns the raw UID as a string.
        /// </summary>
        public string UidStr { get; private set; }

        /// <summary>
        /// Returns the looked-up description for the UID (if one exists, otherwise it's just a copy of the UidStr field.)
        /// </summary>
        public string Desc { get; private set; }

        internal static Dictionary<string, Uid> MasterLookup = new Dictionary<string, Uid>();

        static Uid()
        {
            //force abstract and transfer syntaxes to init to poulate the masterlookup
            AbstractSyntaxes.Init();
            TransferSyntaxes.Init();
        }
    }
}
