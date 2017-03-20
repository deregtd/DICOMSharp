using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DICOMSharp.Data;
using System.IO;
using DICOMSharp.Data.Tags;
using DICOMSharp.Logging;
using System.Security.Cryptography;
using System.Threading;

namespace DICOMSharp.Util
{
    /// <summary>
    /// This static helper class provides some simple helper functions for handling DICOM files.
    /// </summary>
    public class FileUtil
    {
        private FileUtil() { }

        /// <summary>
        /// This is a callback handler for the <see cref="ParseAndLoadImagesFromDirectoryRecursively"/> function.
        /// It is called every time an image is found by the function.
        /// </summary>
        /// <param name="data">The found and loaded/parsed DICOM instance.</param>
        /// <param name="path">The full filename of the source file on disk.</param>
        public delegate void ImageAddedHandler(DICOMData data, string path);

        /// <summary>
        /// Call this function to recursively look through a directory and try to find all valid DICOM files from
        /// that directory.  When it finds one, it will load it (without the image data loaded, for speed reasons)
        /// and pass it through to the <see cref="ImageAddedHandler"/> callback.
        /// </summary>
        /// <param name="baseDir">The directory to start searching in.</param>
        /// <param name="handler">The callback function called with every found/loaded image.</param>
        /// <param name="logger">The logger to use for error/info.</param>
        /// <param name="token">A cancellation token to abort the import.</param>
        public static void ParseAndLoadImagesFromDirectoryRecursively(DirectoryInfo baseDir, ImageAddedHandler handler, ILogger logger, CancellationToken token)
        {
            foreach (DirectoryInfo di2 in baseDir.GetDirectories())
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ParseAndLoadImagesFromDirectoryRecursively(di2, handler, logger, token);
            }

            foreach (FileInfo fi in baseDir.GetFiles())
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    DICOMData data = new DICOMData();
                    if (data.ParseFile(fi.FullName, false, logger))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        handler(data, fi.FullName);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// This will generate a deep folder structure filename for a DICOMData.  The returned format is:
        /// [Study Instance UID]\[Series Instance UID]\[4 digit zero-padded image Instance Number]_[MD5 hash of the SOP Instance UID].dcm
        /// </summary>
        /// <param name="data">The DICOM dataset to generate the filename for.</param>
        /// <param name="logger">A logger to return errors attempting to generate a filename.</param>
        /// <returns></returns>
        public static string GenerateFilenameFromImage(DICOMData data, ILogger logger)
        {
            string studyUID = data[DICOMTags.StudyInstanceUID].Display;
            if (studyUID == "")
            {
                logger.Log(LogLevel.Warning, "Image has no Study Instance UID.  Can not generate filename...");
                return null;
            }

            string seriesUID = data[DICOMTags.SeriesInstanceUID].Display;
            if (seriesUID == "")
            {
                logger.Log(LogLevel.Warning, "Image has no Series Instance UID.  Can not generate filename...");
                return null;
            }

            string instanceUID = data[DICOMTags.SOPInstanceUID].Display;
            if (instanceUID == "")
            {
                logger.Log(LogLevel.Warning, "Image has no SOP Instance UID.  Can not generate filename...");
                return null;
            }

            //form full file path
            string finalPath = studyUID + "\\";
            finalPath += seriesUID + "\\";

            string instanceStr = "UNK";
            if (data.Elements.ContainsKey(DICOMTags.InstanceNumber))
            {
                string imageInstanceStr = data[DICOMTags.InstanceNumber].Display;
                if (!string.IsNullOrEmpty(imageInstanceStr))
                {
                    int instanceNum = 0;
                    if (int.TryParse(imageInstanceStr, out instanceNum))
                    {
                        instanceStr = instanceNum.ToString().PadLeft(4, '0');
                    }
                }
            }

            finalPath += instanceStr + "_";

            byte[] md5instance = new MD5CryptoServiceProvider().ComputeHash(
                Encoding.ASCII.GetBytes(instanceUID));
            finalPath += StringUtil.MakeByteHexString(md5instance) + ".dcm";

            return finalPath;
        }
    }
}
