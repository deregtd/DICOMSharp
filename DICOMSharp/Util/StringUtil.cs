using System.Security.Cryptography;
using System.Text;

namespace DICOMSharp.Util
{
    /// <summary>
    /// This static helper class contains helper functions for manipulating strings.
    /// </summary>
    public class StringUtil
    {
        private StringUtil()
        {
        }

        /// <summary>
        /// This will convert an AE title to ASCII bytes, but will also make sure it turns into 16 characters.
        /// </summary>
        /// <param name="AE">The AE title to convert.</param>
        /// <returns>The converted AE title as a 16-byte array.</returns>
        internal static byte[] ConvertAEToBytes(string AE)
        {
            if (AE.Length > 16)
                AE = AE.Substring(0, 16);

            return Encoding.ASCII.GetBytes(AE.PadRight(16, ' '));
        }

        /// <summary>
        /// This will take any array of bytes and convert it to a hex string.  Example:
        /// Input: MakeByteHexString(new byte[] { 0x01, 0xAB, 0x43 });
        /// Output: "01AB43"
        /// </summary>
        /// <param name="inString">The byte array to convert to a hex string.</param>
        /// <returns>The converted hex string, with no separators between bytes.</returns>
        public static string MakeByteHexString(byte[] inString)
        {
            var sb = new StringBuilder();
            foreach (byte b in inString)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        /// <summary>
        /// Calculate a non-salted MD5 hash of an input string
        /// </summary>
        /// <param name="inputString">The string to hash</param>
        /// <returns>The MD5 hash of the string as a capital-letter hex string</returns>
        public static string MD5String(string inputString)
        {
            // step 1, calculate MD5 hash from input
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(inputString);
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(inputBytes);
            return MakeByteHexString(hash);
        }

        /// <summary>
        /// For elements such as Window center/width that may have VM of 1-n, grab the first element
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string GetFirstFromPossibleNMultiplicity(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }
            var splits = inputString.Split('\\');
            return splits[0];
        }
    }
}
