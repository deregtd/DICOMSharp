using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DICOMSharp.Network.QueryRetrieve
{
    /// <summary>
    /// This enum represents the QueryRetrieve level of a <see cref="QRRequestData"/> structure.
    /// </summary>
    public enum QRLevelType
    {
        /// <summary>
        /// At the Patient level, responses should only involve Patient-level objects.
        /// </summary>
        Patient,

        /// <summary>
        /// At the Study level, responses should involve Study-level objects that may also contain
        /// Patient-level data.
        /// </summary>
        Study,

        /// <summary>
        /// At the Series level, responses should involve Series-level objects but may also contain
        /// Study- and Patient-level data.
        /// </summary>
        Series,

        /// <summary>
        /// At the Image level, responses should involve Instance-level objects but may also contain
        /// Series-, Study-, and Patient-level data.
        /// </summary>
        Image
    }
}
